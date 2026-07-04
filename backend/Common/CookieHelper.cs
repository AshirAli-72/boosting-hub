namespace BoostingHub.backend.Common;

public static class CookieHelper
{
    public static void SetRefreshTokenCookie(HttpResponse response, string token, DateTime expires, int? userId = null)
    {
        response.Cookies.Append("X-Refresh-Token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            IsEssential = true
        });
    }

    public static void RemoveRefreshTokenCookie(HttpResponse response, int? userId = null)
    {
        response.Cookies.Delete("X-Refresh-Token");
    }
}
