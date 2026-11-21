using System.Data;
using Dapper;

namespace KeranjangHijau.Infrastructure.Data;

/// <summary>
/// Npgsql membaca <c>TIMESTAMPTZ</c> sebagai <see cref="DateTime"/> (Kind = Utc), sedangkan model
/// memakai <see cref="DateTimeOffset"/> (sama dengan <c>IClock.UtcNow</c>). Handler ini dipakai Dapper
/// saat MEMBACA kolom ke properti <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// PENTING: untuk parameter, Dapper memakai pemetaan bawaannya (bukan <see cref="SetValue"/>), dan
/// Npgsql menolak offset selain nol untuk TIMESTAMPTZ. Repository wajib mengirim nilai
/// <c>.ToUniversalTime()</c>.
/// </remarks>
internal sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto.ToUniversalTime(),
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        _ => throw new DataException($"Tidak bisa mengubah {value.GetType().Name} menjadi DateTimeOffset."),
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.DateTimeOffset;
        parameter.Value = value.ToUniversalTime();
    }
}
