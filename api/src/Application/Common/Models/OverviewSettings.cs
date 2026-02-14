namespace api.Application.Common.Models;

public class OverviewSettings
{
    public const string SectionName = "OverviewSettings";
    public int StaleDays { get; set; } = 5;
}
