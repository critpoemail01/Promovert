using Promovert.Models;

namespace Promovert.Services;

public interface IMarketDataService
{
    Task<MarketSnapshot> GetSnapshotAsync(string symbol, MarketType marketType, CancellationToken ct);
}
