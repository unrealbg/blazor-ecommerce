namespace BuildingBlocks.Application.Authorization;

public static class BackofficePolicyNames
{
    public const string StaffAccess = "backoffice.staff";

    public static string Permission(string permission)
    {
        return $"permission:{permission}";
    }

    public static bool TryParsePermission(string policyName, out string permission)
    {
        const string Prefix = "permission:";
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            permission = policyName[Prefix.Length..];
            return true;
        }

        permission = string.Empty;
        return false;
    }
}
