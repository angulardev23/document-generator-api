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

        ApiErrorResponse response;
        var statusCode = StatusCodes.Status500InternalServerError;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                response = new ApiErrorResponse(
                    "validation_error",
                    validationException.Message,
                    traceId,
                    validationException.Errors
                        .Select(error => new ApiValidationError(error.Field, error.Message))
                        .ToArray());
                logger.LogInformation(exception, "Validation failed for trace {TraceId}.", traceId);
                break;

            case BadHttpRequestException badHttpRequestException:
                statusCode = StatusCodes.Status400BadRequest;
                response = new ApiErrorResponse(
                    "bad_request",
                    badHttpRequestException.Message,
                    traceId);
                logger.LogInformation(exception, "Bad request for trace {TraceId}.", traceId);
                break;

            case DocumentProcessingException:
                response = new ApiErrorResponse(
                    "document_processing_error",
                    "The document could not be generated.",
                    traceId);
                logger.LogError(exception, "Document generation failed for trace {TraceId}.", traceId);
                break;

            default:
                response = new ApiErrorResponse(
                    "internal_server_error",
                    hostEnvironment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred.",
                    traceId);
                logger.LogError(exception, "Unhandled exception for trace {TraceId}.", traceId);
                break;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}

