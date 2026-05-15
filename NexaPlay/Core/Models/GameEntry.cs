using NexaPlay.Core.Enums;

namespace NexaPlay.Core.Models;

/// <summary>Game metadata from steam_data.json.gz</summary>
public sealed class GameEntry
{
    public int AppId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public string? Genre { get; set; }
    public int PriceNormalized { get; set; }
    public bool Protection { get; set; }

    public bool IsPremium => PriceNormalized >= 130000;
    public bool HasDenuvo => Protection;

    public string CapsuleImageUrl => $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/capsule_231x87.jpg";
    
    private string? _headerImageUrl;
    public string HeaderImageUrl 
    { 
        get => _headerImageUrl ?? $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/header.jpg";
        set => _headerImageUrl = value;
    }
}
