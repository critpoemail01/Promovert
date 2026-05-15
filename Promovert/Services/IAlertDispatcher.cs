using Promovert.Models;

namespace Promovert.Services;

public interface IAlertDispatcher
{
    Task DispatchAsync(Alert alert, MarketSnapshot snapshot, string message, CancellationToken ct);
}
