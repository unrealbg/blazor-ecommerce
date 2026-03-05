using Microsoft.AspNetCore.Http;

namespace Storefront.Web.Services.Customer;

public sealed class CookieCustomerContext(IHttpContextAccessor httpContextAccessor) : ICustomerContext
{
    public const string CookieName = "sf-customer-id";

    public string GetCustomerId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return Guid.NewGuid().ToString("N");
        }

        if (context.Request.Cookies.TryGetValue(CookieName, out var cookieValue) &&
            !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue;
        }

        var generatedValue = Guid.NewGuid().ToString("N");

        context.Response.Cookies.Append(
            CookieName,
            generatedValue,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddYears(1),
            });

        return generatedValue;
    }
}
