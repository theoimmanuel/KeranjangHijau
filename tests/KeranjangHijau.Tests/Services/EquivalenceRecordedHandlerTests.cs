using KeranjangHijau.Core.Dtos;
using KeranjangHijau.Core.Enums;
using KeranjangHijau.Core.Interfaces;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Core.Rules;
using KeranjangHijau.Core.Services;
using KeranjangHijau.Tests.Fakes;

namespace KeranjangHijau.Tests.Services;

public class EquivalenceRecordedHandlerTests
{
    private static readonly UserDto Pengguna = new(10, "Budi", "budi@contoh.id", UserRole.Pengguna, DateTimeOffset.UnixEpoch);

    private readonly InMemoryEquivalenceRepository _repository = new();
    private readonly FakeDbTransaction _transaction = new();

    private EquivalenceRecordedHandler CreateHandler() => new(_repository, new EquivalenceCalculator());

    private static EmissionRecordedContext Context(int recordId = 7, decimal totalCo2eKg = 1.5m)
    {
        var shipment = new Shipment { ShipmentId = 3, NomorResi = "JNE0000000001" };
        var record = new EmissionRecord { RecordId = recordId, ShipmentId = 3, TotalCo2eKg = totalCo2eKg };
        var result = new EmissionResultDto(
            recordId, 3, "JNE0000000001", "jne", "JNE", "Jakarta", "Bandung", "REG", 1m,
            WeightSource.Api, ShipmentDataSource.Mock, 150m, TransportMode.Truk, totalCo2eKg,
            EmissionMethod.RumusLokal, DateTimeOffset.UnixEpoch);
        return new EmissionRecordedContext(Pengguna, shipment, record, result);
    }

    private void AddEquivalences()
    {
        // Angka test (bukan angka seed).
        _repository.Equivalences.Add(new Equivalence
        {
            EquivalenceId = 1, Aktivitas = "Aktivitas A", Satuan = "km", KgCo2ePerSatuan = 0.25m, Ikon = "a",
            Sumber = "PLACEHOLDER - ganti dari jurnal",
        });
        _repository.Equivalences.Add(new Equivalence
        {
            EquivalenceId = 2, Aktivitas = "Aktivitas B", Satuan = "km", KgCo2ePerSatuan = 0.4m, Ikon = null,
            Sumber = "Jurnal X (2025)",
        });
    }

    [Fact]
    public async Task SavesRecordEquivalences_ForRecord_InGivenTransaction()
    {
        AddEquivalences();

        await CreateHandler().HandleAsync(Context(recordId: 7, totalCo2eKg: 1.5m), _transaction);

        // 1,5 / 0,25 = 6 ; 1,5 / 0,4 = 3,75
        Assert.Collection(
            _repository.Rows,
            r => Assert.Equal((7, 1, 6m), (r.RecordId, r.EquivalenceId, r.NilaiSetara)),
            r => Assert.Equal((7, 2, 3.75m), (r.RecordId, r.EquivalenceId, r.NilaiSetara)));
        Assert.Same(_transaction, Assert.Single(_repository.GetAllTransactions));
        Assert.Same(_transaction, Assert.Single(_repository.InsertTransactions));
    }

    [Fact]
    public async Task AddsEquivalenceDtos_ToResult_WithPlaceholderFlag()
    {
        AddEquivalences();
        var context = Context(totalCo2eKg: 1.5m);
        var original = context.Result;

        await CreateHandler().HandleAsync(context, _transaction);

        Assert.Equal(
            [
                new EquivalenceDto("Aktivitas A", "km", 6m, "a", true),
                new EquivalenceDto("Aktivitas B", "km", 3.75m, null, false),
            ],
            context.Result.KonversiSetara);
        Assert.Empty(original.KonversiSetara);
        Assert.Equal(original with { KonversiSetara = context.Result.KonversiSetara }, context.Result);
    }

    [Fact]
    public async Task RoundsToTwoDecimals_BeforeSaving()
    {
        _repository.Equivalences.Add(new Equivalence { EquivalenceId = 1, Aktivitas = "A", Satuan = "km", KgCo2ePerSatuan = 0.3m, Sumber = "x" });

        await CreateHandler().HandleAsync(Context(totalCo2eKg: 1m), _transaction);

        Assert.Equal(3.33m, Assert.Single(_repository.Rows).NilaiSetara);
    }

    [Fact]
    public async Task ZeroFactor_IsNotSaved_NorShown()
    {
        AddEquivalences();
        _repository.Equivalences.Add(new Equivalence { EquivalenceId = 3, Aktivitas = "Rusak", Satuan = "km", KgCo2ePerSatuan = 0m, Sumber = "x" });
        var context = Context();

        await CreateHandler().HandleAsync(context, _transaction);

        Assert.DoesNotContain(_repository.Rows, r => r.EquivalenceId == 3);
        Assert.DoesNotContain(context.Result.KonversiSetara, d => d.Aktivitas == "Rusak");
        Assert.Equal(2, _repository.Rows.Count);
    }

    [Fact]
    public async Task NoEquivalences_SavesNothing_AndResultListIsEmpty()
    {
        var context = Context();

        await CreateHandler().HandleAsync(context, _transaction);

        Assert.Empty(_repository.Rows);
        Assert.Empty(context.Result.KonversiSetara);
    }

    [Fact]
    public async Task RepositoryFailure_Propagates()
    {
        AddEquivalences();
        _repository.FailOnInsert = new InvalidOperationException("gagal simpan");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateHandler().HandleAsync(Context(), _transaction));
    }

    [Fact]
    public async Task NullArguments_Throw()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!, _transaction));
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(Context(), null!));
        Assert.Throws<ArgumentNullException>(() => new EquivalenceRecordedHandler(null!, new EquivalenceCalculator()));
        Assert.Throws<ArgumentNullException>(() => new EquivalenceRecordedHandler(_repository, null!));
    }

    // ---------------------------------------------------------------- EmissionRecordedContext.Result

    [Fact]
    public void ContextResult_CannotBeReplacedWithOtherRecord_OrNull()
    {
        var context = Context(recordId: 7);

        Assert.Throws<ArgumentException>(() => context.Result = context.Result with { RecordId = 8 });
        Assert.Throws<ArgumentNullException>(() => context.Result = null!);
        Assert.Equal(7, context.Result.RecordId);
    }
}
