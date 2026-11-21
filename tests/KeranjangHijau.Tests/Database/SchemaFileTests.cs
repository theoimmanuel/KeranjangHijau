using System.Text.RegularExpressions;

namespace KeranjangHijau.Tests.Database;

/// <summary>
/// Test teks sederhana terhadap file SQL (tanpa koneksi database).
/// </summary>
public class SchemaFileTests
{
    public static TheoryData<string> TableNames =>
    [
        "users",
        "couriers",
        "cities",
        "shipments",
        "emission_factors",
        "emission_records",
        "equivalences",
        "record_equivalences",
        "forests",
        "forest_logs",
        "shared_reports",
    ];

    [Theory]
    [MemberData(nameof(TableNames))]
    public void SchemaSql_CreatesTable(string table)
    {
        var sql = ReadSqlWithoutComments("schema.sql");

        Assert.Matches(new Regex($@"\bCREATE\s+TABLE\s+{table}\s*\(", RegexOptions.IgnoreCase), sql);
    }

    [Fact]
    public void SchemaSql_CreatesExactlyElevenTables()
    {
        var sql = ReadSqlWithoutComments("schema.sql");

        var count = Regex.Matches(sql, @"\bCREATE\s+TABLE\b", RegexOptions.IgnoreCase).Count;

        Assert.Equal(11, count);
    }

    [Fact]
    public void SchemaSql_DoesNotEnforceUniqueResi_DuringDevelopment()
    {
        // CLAUDE.md 7.6: UNIQUE (courier_id, nomor_resi) hanya ada di enable-unique-resi.sql.
        var pattern = new Regex(@"UNIQUE\s*\(\s*courier_id\s*,\s*nomor_resi\s*\)", RegexOptions.IgnoreCase);

        Assert.DoesNotMatch(pattern, ReadSqlWithoutComments("schema.sql"));
        Assert.Matches(pattern, ReadSqlWithoutComments("enable-unique-resi.sql"));
    }

    private static string ReadSqlWithoutComments(string fileName)
    {
        var path = RepoFiles.Path("database", fileName);
        var text = File.ReadAllText(path);
        return Regex.Replace(text, @"--[^\r\n]*", string.Empty);
    }
}
