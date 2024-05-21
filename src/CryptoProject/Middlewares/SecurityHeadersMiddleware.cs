namespace CryptoProject.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self';");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer-when-downgrade");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(self)");
            
            
            //context.Response.Headers.Add("Content-Security-Policy", "default-src 'self';");
            //context.Response.Headers.Append("X-Frame-Options", "DENY");
            //context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            //context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            //context.Response.Headers.Add("Referrer-Policy", "no-referrer-when-downgrade");
            //context.Response.Headers.Add("Permissions-Policy", "geolocation=(self)");

            await _next(context);
        }
    }

}
