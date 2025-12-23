using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ProductIntelligence.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches all exceptions and returns RFC 7807 ProblemDetails responses
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(context, validationException),
            KeyNotFoundException notFoundException => CreateProblemDetails(
                context,
                HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundException.Message),
            InvalidOperationException invalidOpException => CreateProblemDetails(
                context,
                HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidOpException.Message),
            UnauthorizedAccessException => CreateProblemDetails(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource"),
            ArgumentException argumentException => CreateProblemDetails(
                context,
                HttpStatusCode.BadRequest,
                "Bad Request",
                argumentException.Message),
            _ => CreateProblemDetails(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An unexpected error occurred. Please try again later.")
        };

        // Add stack trace in development
        if (_environment.IsDevelopment() && exception is not ValidationException)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
        }

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = (int)problemDetails.Status!;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ValidationProblemDetails(errors)
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred",
            Instance = context.Request.Path,
            Type = "https://httpstatuses.com/400"
        };
    }
}

/// <summary>
/// Extension methods for registering the global exception handler
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
