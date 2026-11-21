using System.Data;
using KeranjangHijau.Core.Interfaces;

namespace KeranjangHijau.Tests.Fakes;

/// <summary><see cref="IEmissionRecordedHandler"/> yang mencatat pemanggilan, dan bisa disuruh gagal.</summary>
public sealed class RecordingEmissionHandler : IEmissionRecordedHandler
{
    private readonly List<string>? _callOrder;
    private readonly string _name;

    public RecordingEmissionHandler(string name = "handler", List<string>? callOrder = null)
    {
        _name = name;
        _callOrder = callOrder;
    }

    public List<(EmissionRecordedContext Context, IDbTransaction Transaction)> Calls { get; } = [];

    /// <summary>Jika diisi, <see cref="HandleAsync"/> melempar exception ini setelah mencatat.</summary>
    public Exception? FailWith { get; set; }

    public Task HandleAsync(EmissionRecordedContext context, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        Calls.Add((context, transaction));
        _callOrder?.Add(_name);
        return FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);
    }
}
