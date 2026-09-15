namespace NexaPlay.Contracts.Services;

public interface ICatalogBootstrapService
{
    string BundledSeedPath { get; }
    string RuntimeDatabasePath { get; }
    Task EnsureReadyAsync(CancellationToken ct = default);
}
