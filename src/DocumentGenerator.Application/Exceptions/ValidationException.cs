namespace DocumentGenerator.Application.Exceptions;

public sealed class ValidationException(IReadOnlyCollection<ValidationError> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyCollection<ValidationError> Errors { get; } = errors;
}

