using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Promovert.Services;

public sealed class UrlCampaignBriefSuggester : IUrlCampaignBriefSuggester
{
    private const int MaxHtmlCharacters = 300_000;
    private const int MaxAdditionalPages = 3;
    private const int MaxBodySnippets = 18;
    private const int MaxHeadingSnippets = 12;

    private static readonly string[] UsefulInternalLinkHints =
    [
        "about", "sobre", "quem-somos", "empresa",
        "service", "services", "servico", "servicos", "serviço", "serviços",
        "product", "products", "produto", "produtos", "solutions", "solucoes", "soluções",
        "software", "platform", "plataforma", "consultoria", "industrial",
        "sbi", "crm", "mes", "planeamento", "manutencao", "manutenção", "qualidade", "metrologia"
    ];

    private static readonly AppTypeRule[] Rules =
    [
        new("Industrial / manufacturing", ["industrial", "industry", "factory", "manufacturing", "production", "produção", "operacoes", "operações", "operations", "maintenance", "manutenção", "energy", "energia", "planning", "planeamento", "mes"],
            "industrial SMEs, factories, production managers, maintenance teams and operations directors",
            "turn operational problems into measurable improvements in production, maintenance, planning or costs",
            "qualified industrial leads and diagnostic requests",
            "practical, concrete and results-focused",
            ["LinkedIn", "Email", "Facebook", "Instagram"]),

        new("Digital transformation / software consulting", ["transformação digital", "transformacao digital", "digital transformation", "soluções de software", "solucoes de software", "software", "aplicações móveis", "aplicacoes moveis", "integração de sistemas", "integracao de sistemas", "consultoria", "iot", "big data", "data science", "sbi", "digitalização", "digitalizacao"],
            "companies that need digital transformation, custom software, system integration, process automation or industrial management tools",
            "turn business processes into integrated digital workflows with custom software, automation, data and operational platforms",
            "qualified project enquiries and discovery calls",
            "consultative, practical and innovation-focused",
            ["LinkedIn", "Email", "Facebook", "Instagram"]),

        new("B2B SaaS / automation", ["crm", "automation", "automação", "automatização", "workflow", "pipeline", "dashboard", "saas", "b2b", "processos", "integração", "integracao", "sistemas"],
            "B2B teams, founders, operations managers and sales teams that need repeatable processes",
            "reduce manual work, organize recurring workflows and make acquisition or operations measurable",
            "demo bookings and trial signups",
            "clear, practical and business-focused",
            ["LinkedIn", "Instagram", "TikTok", "Facebook"]),

        new("AI product", ["ai", "artificial intelligence", "inteligência artificial", "inteligencia artificial", "llm", "chatbot", "agent", "prompt", "machine learning", "automation", "automação", "automatização"],
            "teams and professionals looking for AI-assisted productivity or automation",
            "turn manual tasks into faster AI-assisted workflows with clear business outcomes",
            "trial signups and product demos",
            "modern, educational and direct",
            ["LinkedIn", "X", "YouTube Shorts", "TikTok"]),

        new("Ecommerce / online store", ["shop", "store", "cart", "checkout", "buy", "shipping", "product", "ecommerce", "commerce"],
            "online shoppers and buyers interested in practical offers, product quality and fast purchase decisions",
            "make the product offer easy to understand, compare and buy online",
            "purchases and abandoned-cart recovery",
            "benefit-led, visual and conversion-focused",
            ["Instagram", "TikTok", "Facebook", "YouTube Shorts"]),

        new("Booking / appointments", ["booking", "appointment", "reservation", "schedule", "calendar", "availability", "clinic", "salon"],
            "people who need to book a service, appointment or reservation with less friction",
            "make booking faster, clearer and easier to complete from any device",
            "bookings and qualified enquiries",
            "helpful, local and action-oriented",
            ["Instagram", "Facebook", "TikTok", "LinkedIn"]),

        new("Clinics and healthcare", ["clinic", "doctor", "dentist", "medical", "health", "consultation", "check-up", "appointment"],
            "local patients and families who need a trusted, easy way to book care",
            "make the first contact, check-up or appointment simple and trustworthy",
            "appointments and qualified patient enquiries",
            "trustworthy, clear and local",
            ["Instagram", "Facebook", "TikTok", "Email"]),

        new("Construction and renovation", ["construction", "renovation", "remodel", "builder", "quote", "works"],
            "property owners, companies and managers planning works or renovations",
            "turn project interest into qualified quote requests with proof of completed work",
            "quote requests and site visits",
            "visual, trustworthy and practical",
            ["Instagram", "Facebook", "LinkedIn", "Email"]),

        new("Restaurants and hospitality", ["restaurant", "menu", "food", "reservation", "bar", "cafe", "hospitality"],
            "local customers, visitors and groups looking for a place to eat or book",
            "make menus, reservations and private events easy to discover and book",
            "reservations and event enquiries",
            "visual, local and appetite-led",
            ["Instagram", "Facebook", "TikTok", "Email"]),

        new("Real estate", ["real estate", "property", "house", "apartment", "valuation"],
            "property owners, buyers and investors looking for trusted market guidance",
            "capture valuation requests, property listings and qualified buyer interest",
            "valuation requests and qualified property leads",
            "local, premium and trust-led",
            ["Facebook", "Instagram", "LinkedIn", "Email"]),

        new("Education / course platform", ["course", "learn", "training", "school", "academy", "lesson", "student", "education"],
            "learners, professionals and teams looking for structured training or practical skills",
            "help users learn faster with a clear path, useful content and measurable progress",
            "course registrations and lead capture",
            "educational, encouraging and practical",
            ["YouTube Shorts", "Instagram", "TikTok", "LinkedIn"]),

        new("Finance / invoicing", ["invoice", "payment", "billing", "finance", "accounting", "expense", "bank", "subscription"],
            "business owners, finance teams and operators who need cleaner financial workflows",
            "simplify money-related work, reduce admin and improve visibility over important numbers",
            "demo bookings and account signups",
            "trustworthy, precise and practical",
            ["LinkedIn", "Facebook", "Instagram", "X"]),

        new("Marketing / growth tool", ["marketing", "campaign", "lead", "growth", "social", "email", "sms", "ads", "conversion"],
            "founders, marketers and commercial teams that need more consistent acquisition",
            "plan, launch and measure campaigns across channels without scattered manual work",
            "trial signups and campaign launches",
            "growth-focused, direct and measurable",
            ["LinkedIn", "TikTok", "Instagram", "Facebook"]),

        new("Local services", ["service", "repair", "cleaning", "maintenance", "local", "quote", "estimate", "near me"],
            "local customers searching for reliable service providers and fast responses",
            "make it easy for nearby customers to understand the offer and request contact",
            "qualified enquiries and calls",
            "local, trustworthy and direct",
            ["Facebook", "Instagram", "TikTok", "LinkedIn"])
    ];

    private readonly HttpClient _httpClient;

    public UrlCampaignBriefSuggester(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UrlCampaignBriefSuggestion> SuggestAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(url);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Use a valid http or https URL.");

        await GuardAgainstPrivateHostAsync(uri, cancellationToken);

        var pages = new List<PageMetadata>
        {
            await FetchPageMetadataAsync(uri, cancellationToken)
        };

        foreach (var internalPage in ResolveInternalPageLinks(uri, pages[0]).Take(MaxAdditionalPages))
        {
            try
            {
                await GuardAgainstPrivateHostAsync(internalPage, cancellationToken);
                pages.Add(await FetchPageMetadataAsync(internalPage, cancellationToken));
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                // Secondary pages are useful context, but the homepage must still be enough to create a brief.
            }
        }

        return SuggestFromPage(uri, MergeMetadata(pages));
    }

    private async Task<PageMetadata> FetchPageMetadataAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType) && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The URL did not return an HTML page that can be analyzed.");

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (html.Length > MaxHtmlCharacters)
            html = html[..MaxHtmlCharacters];

        return ExtractMetadata(html);
    }

    public static UrlCampaignBriefSuggestion SuggestFromPage(Uri uri, PageMetadata metadata)
    {
        var title = FirstMeaningful(metadata.OgTitle, metadata.ApplicationName, metadata.Title, DomainName(uri));
        var websiteSignals = WebsiteSignals(metadata).ToList();
        var description = FirstMeaningful(metadata.OgDescription, metadata.Description, websiteSignals.FirstOrDefault(), title);
        var body = NormalizeForSearch(string.Join(" ", new[] { title, description, uri.Host, uri.AbsolutePath }.Concat(websiteSignals)));
        var rule = Rules
            .Select(candidate => new { Rule = candidate, Score = candidate.Keywords.Count(keyword => KeywordMatches(body, keyword)) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Rule.Name)
            .First();

        var selected = rule.Score == 0
            ? new AppTypeRule("General online application", [],
                "potential customers who need a clearer and faster way to solve this problem online",
                "explain the product clearly, remove friction and guide users to the next step",
                "signups and qualified enquiries",
                "clear, practical and benefit-led",
                ["LinkedIn", "Instagram", "Facebook", "TikTok"])
            : rule.Rule;

        var productName = Clip(CleanProductName(title, uri), 160);
        var evidence = Clip(BuildEvidence(metadata, description), 360);
        return new UrlCampaignBriefSuggestion(
            productName,
            BuildCompanyOrIdea(productName, selected, metadata, evidence),
            Clip(selected.Audience, 700),
            Clip($"{selected.ValueProposition}. Use these website proof points in the campaign: {evidence}", 700),
            Clip(selected.Goal, 200),
            Clip(selected.Tone, 80),
            selected.Platforms,
            selected.Name,
            uri.ToString(),
            evidence);
    }

    public static PageMetadata ExtractMetadata(string html)
    {
        var readableHtml = RemoveNoisyHtml(html);
        return new PageMetadata(
            CleanHtml(ExtractTitle(html)),
            CleanHtml(ExtractMeta(html, "name", "description")),
            CleanHtml(ExtractMeta(html, "property", "og:title")),
            CleanHtml(ExtractMeta(html, "property", "og:description")),
            CleanHtml(ExtractMeta(html, "name", "application-name")),
            ExtractHeadings(readableHtml),
            ExtractBodySnippets(readableHtml),
            ExtractLinks(html));
    }

    private static PageMetadata MergeMetadata(IReadOnlyList<PageMetadata> pages)
    {
        var homepage = pages[0];
        return new PageMetadata(
            FirstMeaningful(new[] { homepage.Title }.Concat(pages.Select(page => page.Title)).ToArray()),
            FirstMeaningful(new[] { homepage.Description }.Concat(pages.Select(page => page.Description)).ToArray()),
            FirstMeaningful(new[] { homepage.OgTitle }.Concat(pages.Select(page => page.OgTitle)).ToArray()),
            FirstMeaningful(new[] { homepage.OgDescription }.Concat(pages.Select(page => page.OgDescription)).ToArray()),
            FirstMeaningful(new[] { homepage.ApplicationName }.Concat(pages.Select(page => page.ApplicationName)).ToArray()),
            DistinctCleanValues(pages.SelectMany(page => page.Headings), MaxHeadingSnippets, allowShort: true),
            DistinctCleanValues(pages.SelectMany(page => page.BodySnippets), MaxBodySnippets, allowShort: false),
            DistinctCleanValues(pages.SelectMany(page => page.InternalLinks), 80, allowShort: true));
    }

    private static IReadOnlyList<Uri> ResolveInternalPageLinks(Uri baseUri, PageMetadata metadata)
    {
        var candidates = new List<Uri>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var baseKey = UriKey(baseUri);

        foreach (var href in metadata.InternalLinks)
        {
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(baseUri, href, out var candidate) || candidate.Scheme is not ("http" or "https"))
                continue;

            if (!HostMatches(baseUri, candidate) || LooksLikeAsset(candidate.AbsolutePath))
                continue;

            candidate = StripQueryAndFragment(candidate);
            var key = UriKey(candidate);
            if (key == baseKey || !seen.Add(key))
                continue;

            candidates.Add(candidate);
        }

        return candidates
            .OrderByDescending(InternalLinkScore)
            .ThenBy(uri => uri.AbsolutePath.Length)
            .ToList();
    }

    private static IEnumerable<string> WebsiteSignals(PageMetadata metadata)
    {
        return DistinctCleanValues(
            new[] { metadata.OgDescription, metadata.Description }
                .Concat(metadata.Headings)
                .Concat(metadata.BodySnippets),
            MaxBodySnippets + MaxHeadingSnippets,
            allowShort: true);
    }

    private static string BuildEvidence(PageMetadata metadata, string fallback)
    {
        var evidence = DistinctCleanValues(
            metadata.BodySnippets
                .Concat(metadata.Headings)
                .Prepend(metadata.OgDescription)
                .Prepend(metadata.Description),
            4,
            allowShort: true);

        return FirstMeaningful(string.Join(" / ", evidence), fallback);
    }

    private static string BuildCompanyOrIdea(string productName, AppTypeRule selected, PageMetadata metadata, string evidence)
    {
        var focus = Clip(FirstMeaningful(
            metadata.Description,
            metadata.OgDescription,
            metadata.Headings.FirstOrDefault(),
            metadata.BodySnippets.FirstOrDefault(),
            evidence), 170);

        var offer = selected.Name switch
        {
            "Digital transformation / software consulting" => "digital transformation and software consulting for companies",
            "Industrial / manufacturing" => "industrial operations, production or maintenance solutions",
            "B2B SaaS / automation" => "B2B workflow automation and operational software",
            "AI product" => "AI-assisted automation or productivity software",
            "Marketing / growth tool" => "marketing and growth campaign software",
            "Local services" => "local services for customers who need reliable help",
            _ => selected.Name.ToLowerInvariant()
        };

        return Clip($"{productName} provides {offer}. Site focus: {focus}", 400);
    }

    private static string[] ExtractHeadings(string html)
    {
        return Regex.Matches(html, @"<h[1-3]\b[^>]*>(.*?)</h[1-3]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Select(match => CleanHtml(match.Groups[1].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value) && value.Length > 2 && !IsNoisySnippet(value))
            .DistinctBy(NormalizeForSearch)
            .Take(MaxHeadingSnippets)
            .ToArray();
    }

    private static string[] ExtractBodySnippets(string html)
    {
        var blockSeparated = Regex.Replace(
            html,
            @"</?(?:address|article|aside|blockquote|br|dd|div|dl|dt|figcaption|footer|form|h[1-6]|header|hr|li|main|nav|ol|p|section|table|td|th|tr|ul)\b[^>]*>",
            "\n",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var decoded = WebUtility.HtmlDecode(blockSeparated);
        var withoutTags = Regex.Replace(decoded, "<[^>]+>", " ");
        var normalized = Regex.Replace(withoutTags, @"[ \t\f\v]+", " ");
        var chunks = Regex.Split(normalized, @"\n+|(?<=[.!?])\s+");

        return DistinctCleanValues(chunks, MaxBodySnippets, allowShort: false);
    }

    private static string[] ExtractLinks(string html)
    {
        return Regex.Matches(html, @"<a\b[^>]*\bhref\s*=\s*(?:(['""])(.*?)\1|([^>\s]+))", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Select(match => WebUtility.HtmlDecode(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value).Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(120)
            .ToArray();
    }

    private static string RemoveNoisyHtml(string html)
    {
        var withoutComments = Regex.Replace(html, "<!--.*?-->", " ", RegexOptions.Singleline);
        return Regex.Replace(
            withoutComments,
            @"<(script|style|noscript|svg|canvas|iframe)\b[^>]*>.*?</\1>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    private static string[] DistinctCleanValues(IEnumerable<string> values, int maxCount, bool allowShort)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clean = new List<string>();

        foreach (var value in values)
        {
            var cleaned = CleanHtml(value);
            if (string.IsNullOrWhiteSpace(cleaned))
                continue;

            if (!allowShort && cleaned.Length < 28)
                continue;

            if (IsNoisySnippet(cleaned))
                continue;

            if (!seen.Add(NormalizeForSearch(cleaned)))
                continue;

            clean.Add(cleaned);
            if (clean.Count >= maxCount)
                break;
        }

        return clean.ToArray();
    }

    private static bool IsNoisySnippet(string value)
    {
        var normalized = NormalizeForSearch(value);
        if (normalized.Length <= 2)
            return true;

        var noisyPhrases = new[]
        {
            "click here",
            "saber mais",
            "top",
            "latest news",
            "lorem ipsum",
            "follow us",
            "siga nos",
            "politica de privacidade",
            "privacy policy",
            "todos os direitos reservados",
            "powered by",
            "please leave this field empty",
            "por favor escolha uma opcao",
            "chamada para a rede fixa nacional"
        };

        return noisyPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("Add the application URL first.");

        return trimmed.Contains("://", StringComparison.Ordinal) ? trimmed : $"https://{trimmed}";
    }

    private static async Task GuardAgainstPrivateHostAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(uri.Host, out var parsed))
        {
            if (IsPrivateAddress(parsed))
                throw new InvalidOperationException("Use a public application URL.");

            return;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use a public application URL.");

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        if (addresses.Any(IsPrivateAddress))
            throw new InvalidOperationException("Use a public application URL.");
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
            bytes[0] == 127 ||
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168) ||
            (bytes[0] == 169 && bytes[1] == 254);
    }

    private static string ExtractTitle(string html)
    {
        var match = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ExtractMeta(string html, string attributeName, string attributeValue)
    {
        var pattern = $@"<meta\b(?=[^>]*\b{Regex.Escape(attributeName)}\s*=\s*['""]{Regex.Escape(attributeValue)}['""])(?=[^>]*\bcontent\s*=\s*(['""])(.*?)\1)[^>]*>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[2].Value : string.Empty;
    }

    private static string CleanHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = Regex.Replace(decoded, "<.*?>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string FirstMeaningful(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "Online application";
    }

    private static string CleanProductName(string title, Uri uri)
    {
        var parts = Regex.Split(title, @"\s+(?:\||-|·|—|–)\s+")
            .Select(part => Regex.Replace(part.Trim(), @"^(home|homepage|inicio|início)\s+", string.Empty, RegexOptions.IgnoreCase).Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        if (parts.Count > 0)
        {
            var selected = parts
                .OrderBy(part => IsGenericTitlePart(part) ? 1 : 0)
                .ThenBy(part => part.Length)
                .First();

            if (IsGenericTitlePart(selected))
                return DomainName(uri);

            return string.IsNullOrWhiteSpace(selected) ? DomainName(uri) : selected;
        }

        var cleaned = title;
        foreach (var separator in new[] { " | ", " - ", " · ", " — ", " – " })
        {
            var index = cleaned.IndexOf(separator, StringComparison.Ordinal);
            if (index > 1)
            {
                cleaned = cleaned[..index];
                break;
            }
        }

        cleaned = cleaned.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? DomainName(uri) : cleaned;
    }

    private static bool IsGenericTitlePart(string value)
    {
        var normalized = NormalizeForSearch(value);
        return normalized is "home" or "homepage" or "inicio" or "welcome";
    }

    private static bool HostMatches(Uri expected, Uri candidate)
    {
        return string.Equals(NormalizeHost(expected.Host), NormalizeHost(candidate.Host), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHost(string host)
    {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    private static Uri StripQueryAndFragment(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri;
    }

    private static string UriKey(Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        return $"{NormalizeHost(uri.Host)}{(string.IsNullOrWhiteSpace(path) ? "/" : path)}";
    }

    private static bool LooksLikeAsset(string path)
    {
        return Regex.IsMatch(path, @"\.(?:pdf|jpe?g|png|gif|webp|svg|css|js|zip|rar|docx?|xlsx?|pptx?|mp4|mp3|avi|mov)$", RegexOptions.IgnoreCase);
    }

    private static int InternalLinkScore(Uri uri)
    {
        var searchable = NormalizeForSearch(uri.AbsolutePath.Replace('-', ' ').Replace('_', ' '));
        var score = UsefulInternalLinkHints.Count(hint => searchable.Contains(NormalizeForSearch(hint), StringComparison.OrdinalIgnoreCase)) * 10;
        var segmentCount = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

        if (segmentCount <= 2)
            score += 2;

        return score;
    }

    private static string DomainName(Uri uri)
    {
        var host = NormalizeHost(uri.Host);
        var first = host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Online application";
        return string.Join(" ", Regex.Split(first, "[-_]")).Trim();
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }

    private static bool KeywordMatches(string body, string keyword)
    {
        var normalizedKeyword = NormalizeForSearch(keyword);
        return normalizedKeyword.Length <= 3
            ? Regex.IsMatch(body, $@"\b{Regex.Escape(normalizedKeyword)}\b", RegexOptions.IgnoreCase)
            : body.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }

    public sealed record PageMetadata(
        string Title,
        string Description,
        string OgTitle,
        string OgDescription,
        string ApplicationName,
        IReadOnlyList<string> Headings,
        IReadOnlyList<string> BodySnippets,
        IReadOnlyList<string> InternalLinks)
    {
        public PageMetadata(
            string title,
            string description,
            string ogTitle,
            string ogDescription,
            string applicationName)
            : this(title, description, ogTitle, ogDescription, applicationName, [], [], [])
        {
        }
    }

    private sealed record AppTypeRule(
        string Name,
        IReadOnlyList<string> Keywords,
        string Audience,
        string ValueProposition,
        string Goal,
        string Tone,
        IReadOnlyList<string> Platforms);
}
