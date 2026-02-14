using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.Infrastructure.Services;

public class ImportPipelineHostedService(
    ChannelReader<ImportRequest> channelReader,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportPipelineHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessImportAsync(request, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error processing import session {ImportSessionId}", request.ImportSessionId);
            }
        }
    }

    private async Task ProcessImportAsync(ImportRequest request, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var parser = scope.ServiceProvider.GetRequiredService<IXlsxParser>();
        var matcher = scope.ServiceProvider.GetRequiredService<ICandidateMatchingEngine>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        // Set tenant context for data isolation
        tenantContext.RecruitmentId = request.RecruitmentId;

        var session = await db.ImportSessions
            .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId, ct);

        if (session is null)
        {
            logger.LogWarning("Import session {ImportSessionId} not found, skipping", request.ImportSessionId);
            return;
        }

        try
        {
            // 1. Parse XLSX
            using var stream = new MemoryStream(request.FileContent);
            var rows = parser.Parse(stream);

            // 2. Load existing candidates for matching
            var existingCandidates = await db.Candidates
                .Where(c => c.RecruitmentId == request.RecruitmentId)
                .ToListAsync(ct);

            // 3. Get first workflow step for new candidates
            var recruitment = await db.Recruitments
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, ct);

            var firstStep = recruitment?.Steps.OrderBy(s => s.Order).FirstOrDefault();

            int created = 0, updated = 0, errored = 0, flagged = 0;

            foreach (var row in rows)
            {
                try
                {
                    var match = matcher.Match(row, existingCandidates);

                    switch (match.Confidence)
                    {
                        case ImportMatchConfidence.High:
                            // Email match — update profile
                            var emailCandidate = existingCandidates.First(c =>
                                !string.IsNullOrEmpty(c.Email) &&
                                c.Email.Equals(row.Email, StringComparison.OrdinalIgnoreCase));
                            emailCandidate.UpdateProfile(row.FullName, row.PhoneNumber, row.Location, row.DateApplied ?? DateTimeOffset.UtcNow);
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Updated, null));
                            updated++;
                            break;

                        case ImportMatchConfidence.Low:
                            // Name+phone match — flag for review, do NOT update
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Flagged, null));
                            flagged++;
                            break;

                        case ImportMatchConfidence.None:
                            // No match — create new candidate
                            var candidate = Candidate.Create(
                                request.RecruitmentId,
                                row.FullName,
                                row.Email,
                                row.PhoneNumber,
                                row.Location,
                                row.DateApplied ?? DateTimeOffset.UtcNow);

                            if (firstStep is not null)
                            {
                                candidate.RecordOutcome(firstStep.Id, OutcomeStatus.NotStarted, request.CreatedByUserId);
                            }

                            db.Candidates.Add(candidate);
                            existingCandidates.Add(candidate); // Include for dedup in subsequent rows
                            session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Created, null));
                            created++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    session.AddRowResult(new ImportRowResult(row.RowNumber, row.Email, ImportRowAction.Errored, ex.Message));
                    errored++;
                }
            }

            session.MarkCompleted(created, updated, errored, flagged);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import pipeline failed for session {ImportSessionId}", request.ImportSessionId);
            session.MarkFailed(ex.Message);
        }

        await db.SaveChangesAsync(ct);
    }
}
