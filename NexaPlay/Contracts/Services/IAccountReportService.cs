using NexaPlay.Core.Models;

namespace NexaPlay.Contracts.Services;

public interface IAccountReportService
{
    Task<AccountReportResult> ReportAsync(FixEntry entry, CancellationToken ct = default);
}
