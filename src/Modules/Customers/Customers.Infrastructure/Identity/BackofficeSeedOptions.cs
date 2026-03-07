namespace Customers.Infrastructure.Identity;

public sealed class BackofficeSeedOptions
{
    public const string SectionName = "Backoffice:Seed";

    public bool SeedDefaultAdmin { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string DisplayName { get; set; } = "Local Admin";

    public string Department { get; set; } = "Operations";
}
