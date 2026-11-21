using KeranjangHijau.Core.Exceptions;
using KeranjangHijau.Core.Models;
using KeranjangHijau.Infrastructure.Data;

namespace KeranjangHijau.Tests.Integration;

public class ForestRepositoryTests : RepositoryTestBase
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [DatabaseFact]
    public async Task Insert_ThenGetByUserId_ReturnsSameForest()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var forest = NewForest(userId);

        var forestId = await Forests.InsertAsync(forest, Transaction);
        var loaded = await Forests.GetByUserIdAsync(userId, Transaction);

        Assert.True(forestId > 0);
        Assert.NotNull(loaded);
        Assert.Equal(forestId, loaded.ForestId);
        Assert.Equal(userId, loaded.UserId);
        Assert.Equal("Hutan Pengguna Test", loaded.NamaHutan);
        Assert.Equal(100, loaded.JumlahPohonAwal);
        Assert.Equal(100m, loaded.Kesehatan);
        Assert.Equal("Lestari", loaded.Status);
        Assert.Equal(T0, loaded.UpdatedAt);
    }

    [DatabaseFact]
    public async Task GetByUserIdForUpdate_ReturnsSameForest_OrNull()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var otherUserId = await Users.InsertAsync(NewUser(), Transaction);
        var forestId = await Forests.InsertAsync(NewForest(userId), Transaction);

        var locked = await Forests.GetByUserIdForUpdateAsync(userId, Transaction);

        Assert.Equal(forestId, locked!.ForestId);
        Assert.Equal(100m, locked.Kesehatan);
        Assert.Equal(T0, locked.UpdatedAt);
        Assert.Null(await Forests.GetByUserIdForUpdateAsync(otherUserId, Transaction));
    }

    [DatabaseFact]
    public async Task GetByUserId_UserWithoutForest_ReturnsNull()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);

        Assert.Null(await Forests.GetByUserIdAsync(userId, Transaction));
    }

    [DatabaseFact]
    public async Task UpdateHealth_ChangesHealthStatusAndTime()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var forestId = await Forests.InsertAsync(NewForest(userId), Transaction);
        var later = T0.AddDays(2);

        await Forests.UpdateHealthAsync(forestId, 57.25m, "Menipis", later, Transaction);
        var loaded = await Forests.GetByUserIdAsync(userId, Transaction);

        Assert.Equal(57.25m, loaded!.Kesehatan);
        Assert.Equal("Menipis", loaded.Status);
        Assert.Equal(later, loaded.UpdatedAt);
    }

    [DatabaseFact]
    public async Task UpdateHealth_NonUtcOffset_IsStoredAsSameInstant()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        var forestId = await Forests.InsertAsync(NewForest(userId), Transaction);
        var jakarta = new DateTimeOffset(2026, 3, 2, 15, 0, 0, TimeSpan.FromHours(7));

        await Forests.UpdateHealthAsync(forestId, 90m, "Lestari", jakarta, Transaction);
        var loaded = await Forests.GetByUserIdAsync(userId, Transaction);

        Assert.Equal(jakarta, loaded!.UpdatedAt); // instant yang sama
        Assert.Equal(TimeSpan.Zero, loaded.UpdatedAt.Offset);
    }

    [DatabaseFact]
    public async Task UpdateHealth_UnknownForest_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => Forests.UpdateHealthAsync(-1, 50m, "Menipis", T0, Transaction));

        Assert.Equal(ForestRepository.ForestNotFoundMessage, ex.Message);
    }

    [DatabaseFact]
    public async Task Insert_SecondForestForSameUser_ThrowsValidationException()
    {
        var userId = await Users.InsertAsync(NewUser(), Transaction);
        await Forests.InsertAsync(NewForest(userId), Transaction);

        await Assert.ThrowsAsync<ValidationException>(() => Forests.InsertAsync(NewForest(userId), Transaction));
    }

    [DatabaseFact]
    public async Task Insert_UnknownUser_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Forests.InsertAsync(NewForest(-1), Transaction));
    }

    private static Forest NewForest(int userId) => new()
    {
        UserId = userId,
        NamaHutan = "Hutan Pengguna Test",
        JumlahPohonAwal = 100,
        Kesehatan = 100m,
        Status = "Lestari",
        UpdatedAt = T0,
    };
}
