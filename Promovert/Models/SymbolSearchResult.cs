namespace Promovert.Models;

public record SymbolSearchResult(
    string Symbol,
    string Name,
    string Market,
    string? Exchange = null
);
