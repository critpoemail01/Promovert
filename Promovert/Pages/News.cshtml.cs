using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Promovert.Pages;

public sealed class NewsModel : PageModel
{
    public record Post(string Title, string Tag, string Excerpt, DateTime DateUtc);

    public List<Post> Posts { get; } = new()
    {
        new Post(
            "Credit packs for campaign launches",
            "Product",
            "Buy credit packs and keep running campaigns without a monthly subscription.",
            DateTime.UtcNow.AddDays(-7)),
        new Post(
            "Campaign control: cadence and delivery windows",
            "Product",
            "Scheduling was improved to keep outreach inside the selected delivery window.",
            DateTime.UtcNow.AddDays(-14)),
        new Post(
            "Roadmap: social publishing and SMS sending",
            "Roadmap",
            "New channels are on the way. The same campaign workflow, with more places to distribute.",
            DateTime.UtcNow.AddDays(-30)),
    };

    public void OnGet() { }
}
