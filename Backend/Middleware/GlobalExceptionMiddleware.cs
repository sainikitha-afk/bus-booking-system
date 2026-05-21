using System.Net;
using System.Text.Json;
using Backend.Exceptions;

namespace Backend.Middleware
{
    // Sits at the top of the ASP.NET pipeline.
    // Any unhandled exception thrown anywhere in the app lands here.
    // Returns a clean JSON error — no stack traces exposed to the client.
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass the request down the pipeline
                await _next(context);
            }
            catch (BusBookingException ex)
            {
                // Our own typed exceptions — status code is embedded on the exception
                _logger.LogWarning(ex, "[BusBooking] Domain exception: {Message}", ex.Message);
                await WriteErrorResponse(context, ex.StatusCode, ex.Message);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // Two users tried to modify the same row simultaneously
                _logger.LogError(ex, "[DB] Concurrency conflict");
                await WriteErrorResponse(context, 409,
                    "A conflict occurred while saving. Please retry your request.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Constraint violation, FK violation, etc.
                _logger.LogError(ex, "[DB] Update failed");
                await WriteErrorResponse(context, 500,
                    "A database error occurred. Please try again.");
            }
            catch (Exception ex)
            {
                // Catch-all for anything unexpected
                _logger.LogError(ex, "[Unhandled] {Message}", ex.Message);
                await WriteErrorResponse(context, 500,
                    "An unexpected error occurred. Please try again later.");
            }
            // No finally needed here — the pipeline handles response completion
        }

        private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                error   = message,
                status  = statusCode,
                timestamp = DateTime.UtcNow
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
