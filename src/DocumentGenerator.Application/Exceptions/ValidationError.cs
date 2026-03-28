namespace DocumentGenerator.Application.Exceptions;

public sealed record ValidationError(string Field, string Message);

