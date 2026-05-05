using DocumentGenerator.Api.Contracts;
using DocumentGenerator.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace DocumentGenerator.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment hostEnvironment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        var errorDetails = MapException(exception, traceId);

        logger.Log(errorDetails.LogLevel, exception, errorDetails.LogMessage, traceId);

        httpContext.Response.StatusCode = errorDetails.StatusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(errorDetails.Response, cancellationToken);

        return true;
    }

    private ErrorDetails MapException(Exception exception, string traceId) =>
        exception switch
        {
            ValidationException validationException => MapValidationException(validationException, traceId),
            BadHttpRequestException badHttpRequestException => MapBadRequestException(badHttpRequestException, traceId),
            DocumentProcessingException => MapDocumentProcessingException(traceId),
            DocumentSigningException => MapDocumentSigningException(traceId),
            _ => MapUnhandledException(exception, traceId)
        };

    private static ErrorDetails MapValidationException(
        ValidationException exception,
        string traceId) =>
        new(
            StatusCodes.Status400BadRequest,
            LogLevel.Information,
            "Validation failed for trace {TraceId}.",
            new ApiErrorResponse(
                "validation_error",
                exception.Message,
                traceId,
                exception.Errors
                    .Select(error => new ApiValidationError(error.Field, error.Message))
                    .ToArray()));

    private static ErrorDetails MapBadRequestException(
        BadHttpRequestException exception,
        string traceId) =>
        new(
            StatusCodes.Status400BadRequest,
            LogLevel.Information,
            "Bad request for trace {TraceId}.",
            new ApiErrorResponse(
                "bad_request",
                exception.Message,
                traceId));

    private static ErrorDetails MapDocumentProcessingException(string traceId) =>
        new(
            StatusCodes.Status500InternalServerError,
            LogLevel.Error,
            "Document generation failed for trace {TraceId}.",
            new ApiErrorResponse(
                "document_processing_error",
                "The document could not be generated.",
                traceId));

    private static ErrorDetails MapDocumentSigningException(string traceId) =>
        new(
            StatusCodes.Status500InternalServerError,
            LogLevel.Error,
            "Document signing failed for trace {TraceId}.",
            new ApiErrorResponse(
                "document_signing_error",
                "The document was generated but could not be sent to SignWell.",
                traceId));

    private ErrorDetails MapUnhandledException(Exception exception, string traceId) =>
        new(
            StatusCodes.Status500InternalServerError,
            LogLevel.Error,
            "Unhandled exception for trace {TraceId}.",
            new ApiErrorResponse(
                "internal_server_error",
                hostEnvironment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.",
                traceId));

    private sealed record ErrorDetails(
        int StatusCode,
        LogLevel LogLevel,
        string LogMessage,
        ApiErrorResponse Response);
}
