namespace DocumentGenerator.Application.Exceptions;

public sealed class DocumentProcessingException(string message, Exception innerException)
    : Exception(message, innerException);

