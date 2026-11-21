using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Options;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace KeranjangHijau.Tests.Services;

public class ResiValidatorTests
{
    private const string ValidResi = "JP1234567890";

    private readonly InMemoryCourierRepository _couriers = new();
    private readonly InMemoryShipmentRepository _shipments = new();
    private readonly FeatureOptions _features = new() { AllowDuplicateResi = false };

    private ResiValidator CreateValidator() => new(_couriers, _shipments, Options.Create(_features));

    // ---------------------------------------------------------------- Format resi

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Validate_EmptyResi_Throws(string? resi)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync("jne", resi));

        Assert.Equal(ResiFormatRule.RequiredMessage, ex.Message);
        Assert.Equal("NomorResi", ex.Field);
    }

    [Theory]
    [InlineData("JP12345")]
    [InlineData("jp 123 4")]                          // 7 karakter setelah spasi dibuang
    public async Task Validate_TooShort_Throws(string resi)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync("jne", resi));

        Assert.Equal(ResiFormatRule.LengthMessage, ex.Message);
        Assert.Equal("NomorResi", ex.Field);
    }

    [Fact]
    public async Task Validate_TooLong_Throws()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => CreateValidator().ValidateAsync("jne", new string('A', 31)));

        Assert.Equal(ResiFormatRule.LengthMessage, ex.Message);
    }

    [Theory]
    [InlineData("JP-1234567890")]
    [InlineData("JP1234567890#")]
    [InlineData("JP.1234.5678")]
    public async Task Validate_WithSymbol_Throws(string resi)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync("jne", resi));

        Assert.Equal(ResiFormatRule.CharactersMessage, ex.Message);
        Assert.Equal("NomorResi", ex.Field);
    }

    [Fact]
    public async Task Validate_InvalidFormat_DoesNotQueryDuplicates()
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync("jne", "!!"));

        Assert.Equal(0, _shipments.ExistsCallCount);
    }

    // ---------------------------------------------------------------- Kurir

    [Theory]
    [InlineData("tiki")]
    [InlineData("xyz")]
    public async Task Validate_UnknownCourier_Throws(string kodeKurir)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync(kodeKurir, ValidResi));

        Assert.Equal(ResiValidator.CourierUnknownMessage, ex.Message);
        Assert.Equal("KodeKurir", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Validate_EmptyCourier_Throws(string? kodeKurir)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateValidator().ValidateAsync(kodeKurir, ValidResi));

        Assert.Equal(ResiValidator.CourierRequiredMessage, ex.Message);
        Assert.Equal("KodeKurir", ex.Field);
    }

    [Fact]
    public async Task Validate_CourierCodeIsCaseInsensitive()
    {
        var result = await CreateValidator().ValidateAsync("  SiCepat ", ValidResi);

        Assert.Equal("sicepat", result.Courier.KodeKurir);
        Assert.Equal(3, result.Courier.CourierId);
    }

    // ---------------------------------------------------------------- Normalisasi

    [Fact]
    public async Task Validate_NormalizesSpacesAndLowercase()
    {
        var result = await CreateValidator().ValidateAsync("jne", "  jp 1234 5678 90 ");

        Assert.Equal(ValidResi, result.NomorResi);
        Assert.Equal("jne", result.Courier.KodeKurir);
    }

    // ---------------------------------------------------------------- Duplikat

    [Fact]
    public async Task Validate_Duplicate_WhenFlagFalse_ThrowsDuplicateResiException()
    {
        _features.AllowDuplicateResi = false;
        _shipments.Seed(courierId: 1, ValidResi);

        var ex = await Assert.ThrowsAsync<DuplicateResiException>(
            () => CreateValidator().ValidateAsync("jne", "jp1234 567890"));

        Assert.Equal("Resi ini sudah pernah dicatat.", ex.Message);
        Assert.Equal("NomorResi", ex.Field);
        Assert.Equal("jne", ex.KodeKurir);
        Assert.Equal(ValidResi, ex.NomorResi);
    }

    [Fact]
    public async Task Validate_Duplicate_WhenFlagTrue_Passes()
    {
        _features.AllowDuplicateResi = true;
        _shipments.Seed(courierId: 1, ValidResi);

        var result = await CreateValidator().ValidateAsync("jne", ValidResi);

        Assert.Equal(ValidResi, result.NomorResi);
        Assert.Equal(0, _shipments.ExistsCallCount);
    }

    [Fact]
    public async Task Validate_SameResiDifferentCourier_WhenFlagFalse_Passes()
    {
        _features.AllowDuplicateResi = false;
        _shipments.Seed(courierId: 1, ValidResi);

        var result = await CreateValidator().ValidateAsync("jnt", ValidResi);

        Assert.Equal("jnt", result.Courier.KodeKurir);
        Assert.Equal(1, _shipments.ExistsCallCount);
    }

    [Fact]
    public async Task Validate_NewResi_WhenFlagFalse_Passes()
    {
        var result = await CreateValidator().ValidateAsync("pos", "PS0000000001");

        Assert.Equal("PS0000000001", result.NomorResi);
        Assert.Equal(1, _shipments.ExistsCallCount);
    }
}
