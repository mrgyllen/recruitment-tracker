using System.Threading.Channels;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Constants;
using api.Infrastructure.Data;
using api.Infrastructure.Data.Interceptors;
using api.Infrastructure.Identity;
using api.Infrastructure.Services;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("apiDb");
        Guard.Against.Null(connectionString, message: "Connection string 'apiDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });


        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<ITenantContext, TenantContext>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddScoped<IDirectoryService, DevDirectoryService>();
        }
        else
        {
            builder.Services.AddScoped<IDirectoryService, EntraIdDirectoryService>();
        }

        // Channel<T> for import pipeline
        var importChannel = Channel.CreateUnbounded<ImportRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
        builder.Services.AddSingleton(importChannel.Reader);
        builder.Services.AddSingleton(importChannel.Writer);

        // Import pipeline services
        builder.Services.AddScoped<IXlsxParser, XlsxParserService>();
        builder.Services.AddScoped<ICandidateMatchingEngine, CandidateMatchingEngine>();
        builder.Services.AddHostedService<ImportPipelineHostedService>();

        // PDF splitting and blob storage services
        builder.Services.AddScoped<IPdfSplitter, PdfSplitterService>();
        builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

        // Azure Blob Storage client
        var blobConnectionString = builder.Configuration.GetValue<string>("BlobStorage:ConnectionString")
            ?? "UseDevelopmentStorage=true";
        builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

        // XLSX column mapping options
        builder.Services.Configure<XlsxColumnMappingOptions>(
            builder.Configuration.GetSection(XlsxColumnMappingOptions.SectionName));

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));
    }
}
