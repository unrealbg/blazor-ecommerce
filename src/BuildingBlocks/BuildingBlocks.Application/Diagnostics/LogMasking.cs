namespace BuildingBlocks.Application.Diagnostics;

public static class LogMasking
{
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim();
        var separatorIndex = normalized.IndexOf('@');
        if (separatorIndex <= 1)
        {
            return "***";
        }

        return $"{normalized[0]}***{normalized[(separatorIndex - 1)..]}";
    }

    public static string? MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length <= 6)
        {
            return "***";
        }

        return $"{normalized[..3]}***{normalized[^3..]}";
    }
}