namespace NexaPlay.Contracts.Services;

public interface ICatalogRefreshState
{
    long Generation { get; }
    long Advance();
}
