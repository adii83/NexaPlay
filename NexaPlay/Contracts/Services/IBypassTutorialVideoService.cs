using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IBypassTutorialVideoService
{
    Task<BypassTutorialVideo> GetTutorialVideoAsync(int appId, GameCategory category, CancellationToken ct = default);
}
