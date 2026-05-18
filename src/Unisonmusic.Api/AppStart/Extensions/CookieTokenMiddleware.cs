namespace Unisonmusic.Api.AppStart.Extensions
{
    public class CookieTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.Headers["Authorization"])
                && context.Request.Cookies.TryGetValue("accessToken", out var token))
            {
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            await _next(context);
        }
    }
}
