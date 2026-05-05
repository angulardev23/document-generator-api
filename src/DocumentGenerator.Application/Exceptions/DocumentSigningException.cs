namespace DocumentGenerator.Application.Exceptions;

public sealed class DocumentSigningException(string message, Exception innerException)
    : Exception(message, innerException);
