using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary>Hasher cepat dan deterministik untuk unit test (BUKAN untuk produksi).</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "fake-hash:";

    public int VerifyCalls { get; private set; }

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string passwordHash)
    {
        VerifyCalls++;
        return passwordHash == Prefix + password;
    }
}
