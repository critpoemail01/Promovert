using Promovert.Models;

namespace Promovert.Services;

public interface ITechnicalIndicatorService
{
    Task<TechnicalIndicatorSnapshot?> GetSnapshotAsync(
        string symbol,
        MarketType marketType,
        string timeframe,
        int rsiPeriod,
        int fastEmaPeriod,
        int slowEmaPeriod,
        CancellationToken ct);
}
