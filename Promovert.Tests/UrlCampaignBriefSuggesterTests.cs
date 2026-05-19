using Promovert.Services;

namespace Promovert.Tests;

public class UrlCampaignBriefSuggesterTests
{
    [Fact]
    public void SuggestFromPage_DetectsApplicationTypeAndBuildsEditableBrief()
    {
        var metadata = new UrlCampaignBriefSuggester.PageMetadata(
            "Acme Growth CRM - Campaign automation",
            "AI campaign software for growth teams, email outreach, social posts and conversion tracking.",
            "",
            "",
            "");

        var suggestion = UrlCampaignBriefSuggester.SuggestFromPage(new Uri("https://acme.example.com"), metadata);

        Assert.Equal("Acme Growth CRM", suggestion.ProductName);
        Assert.Equal("Marketing / growth tool", suggestion.DetectedApplicationType);
        Assert.Contains("founders", suggestion.TargetAudience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("campaign", suggestion.ValueProposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LinkedIn", suggestion.Platforms);
    }

    [Fact]
    public void ExtractMetadata_ReadsTitleAndMetaDescription()
    {
        const string html = """
            <html>
              <head>
                <title>SBI Flow | Industrial automation</title>
                <meta content="Automate sales and operations workflows for industrial SMEs." name="description">
              </head>
            </html>
            """;

        var metadata = UrlCampaignBriefSuggester.ExtractMetadata(html);

        Assert.Equal("SBI Flow | Industrial automation", metadata.Title);
        Assert.Equal("Automate sales and operations workflows for industrial SMEs.", metadata.Description);
    }

    [Fact]
    public void ExtractMetadata_ReadsVisibleBodySignalsAndInternalLinks()
    {
        const string html = """
            <html>
              <head>
                <title>Home Advance - Advance</title>
                <script>window.hidden = 'Ignore this campaign signal';</script>
              </head>
              <body>
                <main>
                  <h1>Transformação Digital</h1>
                  <p>Desenvolvimento de soluções de software ajustadas e otimizadas para empresas.</p>
                  <p>Digitalização e automatização de processos com integração de sistemas.</p>
                  <a href="/sobre">Sobre</a>
                </main>
              </body>
            </html>
            """;

        var metadata = UrlCampaignBriefSuggester.ExtractMetadata(html);

        Assert.Contains("Transformação Digital", metadata.Headings);
        Assert.Contains(metadata.BodySnippets, snippet => snippet.Contains("soluções de software", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(metadata.BodySnippets, snippet => snippet.Contains("automatização de processos", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(metadata.BodySnippets, snippet => snippet.Contains("Ignore this campaign signal", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("/sobre", metadata.InternalLinks);
    }

    [Fact]
    public void SuggestFromPage_DetectsIndustrialCompanyWithoutForcingSoftware()
    {
        var metadata = new UrlCampaignBriefSuggester.PageMetadata(
            "SBI Flow | Industrial operations",
            "Planning, production, maintenance and energy control for industrial SMEs.",
            "",
            "",
            "");

        var suggestion = UrlCampaignBriefSuggester.SuggestFromPage(new Uri("https://sbiflow.example.com"), metadata);

        Assert.Equal("Industrial / manufacturing", suggestion.DetectedApplicationType);
        Assert.Contains("industrial", suggestion.TargetAudience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diagnostic", suggestion.CampaignGoal, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SuggestFromPage_UsesWebsiteBodySignalsInBrief()
    {
        var metadata = new UrlCampaignBriefSuggester.PageMetadata(
            "Home Advance - Advance",
            "",
            "",
            "",
            "",
            [
                "Transformação Digital",
                "Consultoria Industrial"
            ],
            [
                "Desenvolvimento de Soluções de software Ajustadas e Otimizadas",
                "Digitalização e Automatização de Processos",
                "Implementação e Integração do SBI"
            ],
            []);

        var suggestion = UrlCampaignBriefSuggester.SuggestFromPage(new Uri("https://advance.com.pt"), metadata);

        Assert.Equal("Advance", suggestion.ProductName);
        Assert.Equal("Digital transformation / software consulting", suggestion.DetectedApplicationType);
        Assert.True(suggestion.CompanyOrIdea.Length <= 400);
        Assert.Contains("software consulting", suggestion.CompanyOrIdea, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("custom software", suggestion.TargetAudience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SBI", suggestion.ValueProposition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Soluções de software", suggestion.Evidence, StringComparison.OrdinalIgnoreCase);
    }
}
