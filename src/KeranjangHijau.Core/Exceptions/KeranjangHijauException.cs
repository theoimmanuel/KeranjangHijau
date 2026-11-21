namespace KeranjangHijau.Core.Exceptions;

/// <summary>
/// Kelas dasar semua error bisnis KeranjangHijau. <see cref="Exception.Message"/> selalu
/// berbahasa Indonesia dan aman ditampilkan langsung ke pengguna.
/// </summary>
public abstract class KeranjangHijauException : Exception
{
    protected KeranjangHijauException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
