using System.Text;

namespace Catalog.Application.Products;

internal static class SlugGenerator
{
    private const int MaxLength = 200;

    public static string Generate(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lowerInvariant = value.Trim().ToLowerInvariant();
        var lastWasDash = false;

        foreach (var character in lowerInvariant)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }

            if (builder.Length >= MaxLength)
            {
                break;
            }
        }

        var result = builder.ToString().Trim('-');

        return string.IsNullOrWhiteSpace(result) ? "product" : result;
    }
}
