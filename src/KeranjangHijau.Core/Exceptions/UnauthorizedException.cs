namespace KeranjangHijau.Core.Exceptions;

/// <summary>
/// Pengguna belum login atau tidak punya hak akses (mis. bukan admin).
/// Login gagal (email/password salah) memakai <see cref="ValidationException"/>, bukan exception ini.
/// </summary>
public class UnauthorizedException : KeranjangHijauException
{
    public const string DefaultMessage = "Anda tidak memiliki akses untuk melakukan tindakan ini.";

    public UnauthorizedException(string message = DefaultMessage)
        : base(message)
    {
    }
}
