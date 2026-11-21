namespace KeranjangHijau.Core.Exceptions;

/// <summary>Data yang diminta tidak ditemukan (mis. kurir, kota, atau resi tidak dikenal).</summary>
public class NotFoundException : KeranjangHijauException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
