using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentGenerator.Infrastructure.Persistence;

public static class DocumentGeneratorDbContextExceptionExtensions
{
    public static bool IsSerializableConflict(this Exception exception)
    {
        return exception switch
        {
            PostgresException postgresException => postgresException.SqlState == PostgresErrorCodes.SerializationFailure,
            DbUpdateException { InnerException: PostgresException postgresException } =>
                postgresException.SqlState == PostgresErrorCodes.SerializationFailure,
            _ => false
        };
    }
}
