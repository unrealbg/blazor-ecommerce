namespace BuildingBlocks.Infrastructure.Modules;

public static class ReleaseSeedModes
{
    public const string None = "none";
    public const string Minimal = "minimal";
    public const string Demo = "demo";
    public const string Test = "test";

    public static string Normalize(string? seedMode)
    {
        return string.IsNullOrWhiteSpace(seedMode)
            ? None
            : seedMode.Trim().ToLowerInvariant();
    }

    public static bool IsSupported(string? seedMode)
    {
        return Normalize(seedMode) is None or Minimal or Demo or Test;
    }
}
