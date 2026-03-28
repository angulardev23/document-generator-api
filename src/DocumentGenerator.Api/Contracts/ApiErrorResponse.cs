namespace DocumentGenerator.Api.Contracts;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    string TraceId,
    IReadOnlyCollection<ApiValidationError>? Errors = null);

public sealed record ApiValidationError(string Field, string Message);

