using NexaPlay.Contracts.Services;
using System.Collections.Concurrent;

namespace NexaPlay.Infrastructure.Logging;

/// <summary>Thread-safe, in-memory + file application log service.</summary>
public sealed class AppLogService : IAppLogService
{
    private readonly ConcurrentQueue<string> _buffer = new();
    private readonly string _logPath;
    private const int MaxBufferSize = 500;

    public event EventHandler<string>? LogAppended;

    public AppLogService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Core.Constants.AppConstants.AppDataFolder);
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, Core.Constants.AppConstants.LogFileName);
    }

    public void Log(string message) => Log("APP", message);

    public void Log(string category, string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] [{category}] {message}";
        _buffer.Enqueue(entry);
        while (_buffer.Count > MaxBufferSize) _buffer.TryDequeue(out _);

        try { File.AppendAllText(_logPath, entry + Environment.NewLine); } catch { }
        LogAppended?.Invoke(this, entry);
    }

    public IReadOnlyList<string> GetRecentLogs(int count = 200)
        => _buffer.TakeLast(count).ToList();

    public async Task<string> GetFullLogAsync()
    {
        try { return await File.ReadAllTextAsync(_logPath); }
        catch { return string.Join(Environment.NewLine, _buffer); }
    }

    public void Clear()
    {
        while (_buffer.TryDequeue(out _)) { }
        try { File.WriteAllText(_logPath, string.Empty); } catch { }
    }
}
