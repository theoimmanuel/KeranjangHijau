using Npgsql;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>Membaca error PostgreSQL berdasarkan nama constraint di <c>database/schema.sql</c>.</summary>
internal static class PostgresErrors
{
    public static bool IsUniqueViolation(PostgresException ex, string constraintName) =>
        ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == constraintName;

    public static bool IsForeignKeyViolation(PostgresException ex, string constraintName) =>
        ex.SqlState == PostgresErrorCodes.ForeignKeyViolation && ex.ConstraintName == constraintName;
}
