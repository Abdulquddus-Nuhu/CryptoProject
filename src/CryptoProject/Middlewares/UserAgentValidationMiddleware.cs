namespace CryptoProject.Middlewares
{
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class UserAgentValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserAgentValidationMiddleware> _logger;

    public UserAgentValidationMiddleware(RequestDelegate next, ILogger<UserAgentValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        // Check if the User-Agent is suspicious
        if (IsSuspiciousUserAgent(userAgent))
        {
            _logger.LogWarning("Blocked suspicious user agent: {0}. IP-Address: {1}",userAgent, context.Connection.RemoteIpAddress?.ToString());
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied");
            return;
        }

        // Block requests trying to access sensitive files
        if (context.Request.Path.StartsWithSegments("/.env") ||
            context.Request.Path.StartsWithSegments("/.git") ||
            context.Request.QueryString.Value.Contains("wget"))
        {
            _logger.LogWarning("Blocked access to path '/.env' or '/.git' or 'wget' by suspicious user: {0}", context.Connection.RemoteIpAddress?.ToString());
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access to this resource is denied. Fuck you!!!");
            return;
        }


            await _next(context);
    }

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        // Define suspicious patterns here, for example:
        return userAgent.Contains("curl") || userAgent.Contains("python") || userAgent.Contains("scanner") || userAgent.Contains("AVG") || userAgent.Contains("Puffin") || userAgent.Contains("Odin");
    }
}

}
