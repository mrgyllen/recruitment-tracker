namespace api.Infrastructure.Services;

public class XlsxColumnMappingOptions
{
    public const string SectionName = "XlsxColumnMapping";

    public string[] FullName { get; set; } = ["Full Name", "Name", "Candidate Name"];
    public string[] Email { get; set; } = ["Email", "Email Address", "E-mail"];
    public string[] PhoneNumber { get; set; } = ["Phone", "Phone Number", "Tel"];
    public string[] Location { get; set; } = ["Location", "City", "Office"];
    public string[] DateApplied { get; set; } = ["Date Applied", "Application Date", "Applied"];
}
