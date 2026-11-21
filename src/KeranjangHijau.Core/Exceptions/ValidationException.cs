namespace KeranjangHijau.Core.Exceptions;

/// <summary>Input pengguna tidak valid (mis. format resi salah, berat di luar batas).</summary>
public class ValidationException : KeranjangHijauException
{
    public ValidationException(string message, string? field = null)
        : base(message)
    {
        Field = field;
    }

    /// <summary>Nama field yang salah (mis. <c>"NomorResi"</c>), agar UI bisa menandai input-nya.</summary>
    public string? Field { get; }
}
