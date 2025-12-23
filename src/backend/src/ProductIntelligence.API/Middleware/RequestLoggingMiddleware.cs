using System.Diagnostics;

namespace ProductIntelligence.API.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with timing information
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        try
        {
            _logger.LogInformation(
                "Incoming {Method} request to {Path} | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                requestId);

            await _next(context);

            stopwatch.Stop();

            var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(
                logLevel,
                "Completed {Method} request to {Path} | Status: {StatusCode} | Duration: {Duration}ms | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                requestId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request {Method} {Path} failed after {Duration}ms | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                requestId);
            throw;
        }
    }
}

/// <summary>
/// Extension methods for registering request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
