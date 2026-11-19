namespace KeranjangHijau.Tests;

/// <summary>
/// Test contoh untuk memastikan pipeline <c>dotnet test</c> berjalan.
/// Hapus atau ganti setelah test aturan domain pertama ditambahkan.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void TestPipeline_Runs()
    {
        var total = 1 + 1;

        Assert.Equal(2, total);
    }
}
