namespace api.Application.Common.Models;

public sealed record ImportRequest(
    Guid ImportSessionId,
    Guid RecruitmentId,
    byte[] FileContent,
    Guid CreatedByUserId);
