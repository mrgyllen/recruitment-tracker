namespace api.Domain.Exceptions;

public class RecruitmentClosedException : Exception
{
    public RecruitmentClosedException(Guid recruitmentId)
        : base($"Recruitment {recruitmentId} is closed and cannot be modified.")
    {
    }
}
