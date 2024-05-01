namespace CryptoProject.Middlewares
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class NotFoundRequestTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private static ConcurrentDictionary<string, int> _failedRequests = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, Task> _banResetTasks = new ConcurrentDictionary<string, Task>();
        private const int MAX_FAILED_REQUESTS = 10; // Maximum allowed 404 requests
        private const int BAN_TIME_MINUTES = 60; // Duration of the ban in minutes

        public NotFoundRequestTrackingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 404)
            {
                var ipAddress = context.Connection.RemoteIpAddress.ToString();
                _failedRequests.AddOrUpdate(ipAddress, 1, (key, count) => count + 1);

                if (_failedRequests[ipAddress] > MAX_FAILED_REQUESTS)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied due to unusual activity.");
                    return;
                }
                else
                {
                    ResetBanTimer(ipAddress);
                }
            }
        }

        private void ResetBanTimer(string ipAddress)
        {
            // Cancel any existing reset task
            if (_banResetTasks.TryRemove(ipAddress, out var existingTask))
            {
                existingTask.Dispose();
            }

            // Create a new reset task
            var resetTask = Task.Delay(TimeSpan.FromMinutes(BAN_TIME_MINUTES)).ContinueWith(t => {
                _failedRequests.TryRemove(ipAddress, out _);
            });

            _banResetTasks.TryAdd(ipAddress, resetTask);
        }
    }

}
