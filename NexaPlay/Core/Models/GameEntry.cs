using NexaPlay.Core.Enums;

namespace NexaPlay.Core.Models;

/// <summary>Game metadata from steam_data.json.gz</summary>
public sealed class GameEntry
{
    public int AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string? Genre { get; init; }
    public string CapsuleImageUrl => $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/capsule_231x87.jpg";
    public string HeaderImageUrl  => $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/header.jpg";
}
