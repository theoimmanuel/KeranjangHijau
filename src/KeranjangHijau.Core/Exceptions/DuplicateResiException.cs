namespace KeranjangHijau.Core.Exceptions;

/// <summary>
/// Resi yang sama untuk kurir yang sama sudah pernah diinput (CLAUDE.md 7.6).
/// Hanya dilempar saat <c>Features:AllowDuplicateResi = false</c>.
/// </summary>
public class DuplicateResiException : ValidationException
{
    public const string DefaultMessage = "Resi ini sudah pernah dicatat.";

    public DuplicateResiException(string kodeKurir, string nomorResi)
        : base(DefaultMessage, "NomorResi")
    {
        KodeKurir = kodeKurir;
        NomorResi = nomorResi;
    }

    public string KodeKurir { get; }

    public string NomorResi { get; }
}
