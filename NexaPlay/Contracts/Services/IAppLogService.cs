namespace NexaPlay.Contracts.Services;

public interface IAppLogService
{
    event EventHandler<string> LogAppended;
    void Log(string message);
    void Log(string category, string message);
    IReadOnlyList<string> GetRecentLogs(int count = 200);
    Task<string> GetFullLogAsync();
    void Clear();
}
