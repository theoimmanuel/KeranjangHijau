using KeranjangHijau.Core.Exceptions;

namespace KeranjangHijau.Tests.Core;

public class ExceptionTests
{
    [Fact]
    public void DuplicateResiException_IsValidationError_WithIndonesianMessage()
    {
        var ex = new DuplicateResiException("jne", "JP1234567890");

        Assert.IsAssignableFrom<ValidationException>(ex);
        Assert.IsAssignableFrom<KeranjangHijauException>(ex);
        Assert.Equal("NomorResi", ex.Field);
        Assert.Equal("Resi ini sudah pernah dicatat.", ex.Message);
        Assert.Equal("jne", ex.KodeKurir);
        Assert.Equal("JP1234567890", ex.NomorResi);
    }

    [Fact]
    public void UnauthorizedException_HasDefaultMessage()
    {
        Assert.Equal(UnauthorizedException.DefaultMessage, new UnauthorizedException().Message);
    }
}
