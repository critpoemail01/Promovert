using Promovert.Models;

namespace Promovert.Services;

public interface IAlertRuleEngine
{
    RuleResult Evaluate(Alert alert, MarketSnapshot snapshot, TechnicalIndicatorSnapshot? technical = null);
}
