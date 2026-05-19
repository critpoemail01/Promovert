using Promovert.Models;

namespace Promovert.Services;

public sealed class TemplateAiMarketingPlannerService : IAiMarketingPlannerService
{
    private static readonly Dictionary<string, PlatformProfile> PlatformProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TikTok"] = new("TikTok", "short video", "fast hook, visual demo, human tone", "#startup #productivity #saas #founders", 4200, 0.09m, 0.018m),
        ["Instagram"] = new("Instagram", "reel or carousel", "visual story, concise caption, proof point", "#app #digitalbusiness #growth #marketing", 3200, 0.075m, 0.014m),
        ["Facebook"] = new("Facebook", "community post", "problem-led copy, social proof, clear question", "#smallbusiness #onlinebusiness #growth", 2100, 0.052m, 0.010m),
        ["LinkedIn"] = new("LinkedIn", "B2B post", "business context, measurable outcome, professional CTA", "#b2b #saas #growth #automation", 1800, 0.061m, 0.018m),
        ["X"] = new("X", "short thread", "sharp insight, punchy wording, one clear takeaway", "#buildinpublic #saas #growth", 1400, 0.047m, 0.009m),
        ["YouTube Shorts"] = new("YouTube Shorts", "short video", "demo-first script, title promise, retention beat", "#shorts #appdemo #saas", 3600, 0.063m, 0.013m)
    };

    private static readonly string[] Angles =
    {
        "pain point",
        "before and after",
        "product demo",
        "customer objection",
        "founder story",
        "use case",
        "comparison",
        "quick win",
        "social proof",
        "limited offer"
    };

    public AiMarketingPlanDraft Generate(AiMarketingPlanRequest request)
    {
        var schedule = BuildSchedule(request.StartDate, request.EndDate, request.Frequency).ToList();
        var learning = request.CompanyLearning?.HasData == true ? request.CompanyLearning : null;
        var platforms = request.Platforms.Count == 0
            ? new List<string> { "TikTok", "Instagram", "Facebook", "LinkedIn" }
            : request.Platforms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        platforms = OrderPlatforms(platforms, learning).ToList();

        var posts = new List<MarketingPostSuggestion>();
        var dayIndex = 0;

        foreach (var date in schedule)
        {
            dayIndex++;
            foreach (var platform in platforms)
            {
                var profile = PlatformProfiles.TryGetValue(platform, out var configured)
                    ? configured
                    : new PlatformProfile(platform, "social post", "clear hook, proof, CTA", "#growth #marketing", 1200, 0.045m, 0.008m);

                var angle = SelectAngle(dayIndex, platform, learning);
                var scheduledAt = date.ToDateTime(new TimeOnly(DefaultHourFor(profile.Name), 0), DateTimeKind.Utc);
                var reach = ScaleReach(profile.BaseReach + (dayIndex * 37) + (request.ProductName.Length * 11), request.Location);
                var isPreferredPlatform = learning?.PreferredPlatforms.Contains(profile.Name, StringComparer.OrdinalIgnoreCase) == true;
                if (isPreferredPlatform)
                    reach = Math.Max(1, (int)Math.Round(reach * 1.08m));

                var interactions = Math.Max(1, (int)Math.Round(reach * profile.InteractionRate));
                var conversionRate = isPreferredPlatform ? profile.ConversionRate + 0.006m : profile.ConversionRate;
                var conversions = Math.Max(0, (int)Math.Round(interactions * conversionRate));

                posts.Add(BuildPost(request, profile, angle, scheduledAt, dayIndex, reach, interactions, conversions, variation: 0));
            }
        }

        var emails = BuildEmails(request, schedule);
        var leads = BuildLeadSuggestions(request);
        var landingPage = BuildLandingPage(request);
        var businessDna = BuildBusinessDna(request);

        return new AiMarketingPlanDraft(posts, emails, leads, landingPage, businessDna);
    }

    public MarketingPostSuggestion RegeneratePost(AiMarketingPlanRequest request, string platform, DateTime scheduledForUtc, int dayNumber, int variation)
    {
        var profile = PlatformProfiles.TryGetValue(platform, out var configured)
            ? configured
            : new PlatformProfile(platform, "social post", "clear hook, proof, CTA", "#growth #marketing", 1200, 0.045m, 0.008m);

        var learning = request.CompanyLearning?.HasData == true ? request.CompanyLearning : null;
        var angle = SelectAngle(dayNumber + Math.Max(1, variation), $"{platform}-{variation}", learning);
        var reach = ScaleReach(profile.BaseReach + (dayNumber * 41) + (variation * 173) + (request.ProductName.Length * 9), request.Location);
        var preferred = learning?.PreferredPlatforms.Contains(profile.Name, StringComparer.OrdinalIgnoreCase) == true;
        if (preferred)
            reach = Math.Max(1, (int)Math.Round(reach * 1.08m));

        var interactions = Math.Max(1, (int)Math.Round(reach * profile.InteractionRate));
        var conversions = Math.Max(0, (int)Math.Round(interactions * (preferred ? profile.ConversionRate + 0.006m : profile.ConversionRate)));

        return BuildPost(request, profile, angle, scheduledForUtc, dayNumber, reach, interactions, conversions, variation);
    }

    private static MarketingPostSuggestion BuildPost(
        AiMarketingPlanRequest request,
        PlatformProfile profile,
        string angle,
        DateTime scheduledAt,
        int dayNumber,
        int reach,
        int interactions,
        int conversions,
        int variation)
    {
        return new MarketingPostSuggestion
        {
            Platform = Clip(profile.Name, 40),
            ScheduledForUtc = scheduledAt,
            DayNumber = dayNumber,
            Title = Clip($"{request.ProductName}: {TitleForAngle(angle)}{VariationSuffix(variation)}", 140),
            Hook = Clip(HookFor(profile, request, angle, variation), 300),
            Caption = Clip(CaptionFor(profile, request, angle, variation), 1600),
            CreativeBrief = Clip(CreativeBriefFor(profile, request, angle, variation), 900),
            Hashtags = Clip(HashtagsFor(profile, request, variation), 300),
            CallToAction = Clip(CallToActionFor(request), 180),
            Status = "Draft",
            EstimatedReach = reach,
            EstimatedInteractions = interactions,
            EstimatedConversions = conversions
        };
    }

    private static IEnumerable<string> OrderPlatforms(IReadOnlyList<string> platforms, CompanyLearningProfile? learning)
    {
        if (learning is null || learning.PreferredPlatforms.Count == 0)
            return platforms;

        var originalOrder = platforms
            .Select((platform, index) => new { platform, index })
            .ToDictionary(x => x.platform, x => x.index, StringComparer.OrdinalIgnoreCase);

        return platforms
            .OrderBy(platform =>
            {
                var index = learning.PreferredPlatforms
                    .Select((value, i) => new { value, i })
                    .FirstOrDefault(x => x.value.Equals(platform, StringComparison.OrdinalIgnoreCase))
                    ?.i;

                return index ?? int.MaxValue;
            })
            .ThenBy(platform => originalOrder[platform]);
    }

    private static string SelectAngle(int dayIndex, string platform, CompanyLearningProfile? learning)
    {
        if (learning is not null)
        {
            var style = learning.PreferredPostStyle.ToLowerInvariant();
            if (style.Contains("case"))
                return dayIndex % 2 == 0 ? "social proof" : "use case";

            if (style.Contains("educational"))
                return dayIndex % 2 == 0 ? "use case" : "quick win";

            if (style.Contains("demo"))
                return "product demo";

            if (style.Contains("before"))
                return "before and after";
        }

        return Angles[(dayIndex + platform.Length) % Angles.Length];
    }

    private static IEnumerable<DateOnly> BuildSchedule(DateOnly startDate, DateOnly endDate, string frequency)
    {
        if (frequency.Equals("Mvp14", StringComparison.OrdinalIgnoreCase))
        {
            var offsets = new[] { 0, 2, 5, 8, 11 };
            foreach (var offset in offsets)
            {
                var date = startDate.AddDays(offset);
                if (date <= endDate)
                    yield return date;
            }

            yield break;
        }

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var day = date.DayOfWeek;
            var include = frequency switch
            {
                "Weekdays" => day is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
                "ThreePerWeek" => day is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday,
                "Weekly" => date == startDate || day == startDate.DayOfWeek,
                _ => true
            };

            if (include)
                yield return date;
        }
    }

    private static List<MarketingEmailSuggestion> BuildEmails(AiMarketingPlanRequest request, IReadOnlyList<DateOnly> schedule)
    {
        if (schedule.Count == 0)
            return new List<MarketingEmailSuggestion>();

        var learnedShortEmail = request.CompanyLearning?.HasData == true &&
            request.CompanyLearning.EmailStyle.Contains("short", StringComparison.OrdinalIgnoreCase);
        var offsets = new[] { 0, 3, 7, 14, 21, 28 };
        var themes = new[]
        {
            ("Quick intro", "A short, direct opener that explains the problem and why the product exists."),
            ("Pain and cost", "Show the cost of doing nothing, then position the product as the lower-friction path."),
            ("Use case", "Explain one concrete workflow for the target audience."),
            ("Proof and trust", "Show proof, expected outcome and a practical next step."),
            ("Offer", "Make the offer clear and reduce risk."),
            ("Final follow-up", "Short reminder with a human close and one CTA.")
        };

        var emails = new List<MarketingEmailSuggestion>();
        for (var i = 0; i < offsets.Length; i++)
        {
            var date = request.StartDate.AddDays(offsets[i]);
            if (date > request.EndDate)
                continue;

            var theme = themes[i];
                var reach = Math.Max(1, CountAudienceContacts(request.EmailAudience));
                if (reach == 1)
                reach = ScaleReach(80 + (i * 16), request.Location);

            emails.Add(new MarketingEmailSuggestion
            {
                ScheduledForUtc = date.ToDateTime(new TimeOnly(9, 30), DateTimeKind.Utc),
                DayNumber = offsets[i] + 1,
                Subject = Clip(SubjectFor(request, theme.Item1), 160),
                PreviewText = Clip(learnedShortEmail ? $"Short follow-up adapted from previous {request.ProductName} results." : theme.Item2, 220),
                Body = Clip(EmailBodyFor(request, theme.Item1, learnedShortEmail), 4000),
                AudienceSegment = Clip(string.IsNullOrWhiteSpace(request.EmailAudience) ? "Suggested outbound audience" : "Provided potential-client list", 160),
                Status = "Draft",
                EstimatedReach = reach,
                EstimatedInteractions = Math.Max(1, (int)Math.Round(reach * 0.18m)),
                EstimatedConversions = Math.Max(0, (int)Math.Round(reach * 0.035m))
            });
        }

        return emails;
    }

    private static List<MarketingLeadSuggestion> BuildLeadSuggestions(AiMarketingPlanRequest request)
    {
        var audience = request.TargetAudience.ToLowerInvariant();
        string[] industries = audience.Contains("saas") || audience.Contains("b2b")
            ? new[] { "B2B SaaS", "IT services", "Business operations", "Digital agencies", "Consulting" }
            : audience.Contains("restaurant") || audience.Contains("food")
                ? new[] { "Restaurants", "Hospitality groups", "Food delivery brands", "Local franchises", "Event venues" }
                : new[] { "Online services", "Small businesses", "Digital commerce", "Professional services", "Education providers" };

        var roles = new[] { "Founder", "Growth lead", "Marketing manager", "Operations manager", "Sales lead" };
        var leads = new List<MarketingLeadSuggestion>();

        for (var i = 0; i < industries.Length; i++)
        {
            leads.Add(new MarketingLeadSuggestion
            {
                CompanyProfile = Clip($"{industries[i]} company with visible online acquisition needs", 160),
                Industry = Clip(industries[i], 120),
                ContactRole = Clip(roles[i % roles.Length], 120),
                EmailSearchHint = Clip($"Search '{industries[i]} {roles[i % roles.Length]} email' or LinkedIn company pages", 180),
                Reason = Clip($"{request.ProductName} can be positioned around '{request.ValueProposition}' for this segment in {request.Location.Summary}.", 500),
                Status = "Suggested"
            });
        }

        return leads;
    }

    private static MarketingLandingPage BuildLandingPage(AiMarketingPlanRequest request)
    {
        var slug = $"{Slugify(request.ProductName)}-{Guid.NewGuid():N}";
        var learnedFormIntro = request.CompanyLearning?.HasData == true
            ? $"{request.CompanyLearning.LandingPageAdvice}. Leave your details and the team will follow up with the most relevant next step."
            : "Leave your details and the team will follow up with the most relevant next step.";
        var primaryCta = request.CompanyLearning?.HasData == true
            ? request.CompanyLearning.PreferredCta
            : string.IsNullOrWhiteSpace(request.ProductUrl) ? $"Request {request.ProductName}" : $"Start with {request.ProductName}";

        return new MarketingLandingPage
        {
            Slug = slug.Length <= 72 ? slug : slug[..72],
            Headline = Clip($"{request.ProductName} for {request.TargetAudience}", 180),
            Subheadline = Clip($"A focused campaign page built around {request.ValueProposition} for {request.Location.Summary}.", 260),
            Body = Clip(
                $"The campaign positions {request.ProductName} around one clear outcome: {request.CampaignGoal.ToLowerInvariant()}.\n\n" +
                $"Business DNA: {BuildBusinessDna(request)}\n\n" +
                $"Why now: {request.TargetAudience} need a simpler path to {request.ValueProposition.ToLowerInvariant()}.",
                1600),
            PrimaryCallToAction = Clip(primaryCta, 120),
            FormTitle = Clip($"Talk to {request.ProductName}", 160),
            FormIntro = Clip(learnedFormIntro, 260),
            ThankYouMessage = Clip("Thank you. Your request was captured and added to the campaign report.", 260),
            Status = "Draft"
        };
    }

    private static string BuildBusinessDna(AiMarketingPlanRequest request)
    {
        return Clip(
            $"Product: {request.ProductName}. Market: {request.TargetAudience}. Promise: {request.ValueProposition}. " +
            $"Goal: {request.CampaignGoal}. Tone: {request.Tone}. Region: {request.Location.Summary}. " +
            $"Source: {(string.IsNullOrWhiteSpace(request.ProductUrl) ? request.CompanyOrIdea : request.ProductUrl)}" +
            (request.CompanyLearning?.HasData == true ? $". Learning: {request.CompanyLearning.Summary}" : string.Empty),
            1000);
    }

    private static string TitleForAngle(string angle)
    {
        return angle switch
        {
            "pain point" => "the pain blocking the decision",
            "before and after" => "from messy process to measurable result",
            "product demo" => "show the product in action",
            "customer objection" => "answer the biggest objection",
            "founder story" => "why this should exist now",
            "use case" => "a use case to copy",
            "comparison" => "compare the old method with the new one",
            "quick win" => "one practical result today",
            "social proof" => "make the result credible",
            _ => "clarify the offer"
        };
    }

    private static string HookFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle, int variation)
    {
        var location = LocationPhrase(request.Location);
        var opener = (variation % 3) switch
        {
            1 => "New approach:",
            2 => "Try this:",
            _ => string.Empty
        };

        return profile.Name switch
        {
            "TikTok" => $"{opener} Most {request.TargetAudience}{location} lose time here: {request.ValueProposition}.",
            "Instagram" => $"{opener} A simple way to make {request.CampaignGoal.ToLowerInvariant()} less random{location}.",
            "Facebook" => $"{opener} Question for {request.TargetAudience}{location}: what would change if this workflow became automatic?",
            "LinkedIn" => $"{opener} {request.ProductName} turns a common problem for {request.TargetAudience}{location} into a measurable workflow.",
            "X" => $"{opener} The underestimated growth move: make {request.ValueProposition.ToLowerInvariant()} obvious.",
            "YouTube Shorts" => $"{opener} Watch {request.ProductName} solve this in under 30 seconds.",
            _ => $"{opener} {request.ProductName}: {TitleForAngle(angle)}."
        };
    }

    private static string CaptionFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle, int variation)
    {
        var urlLine = string.IsNullOrWhiteSpace(request.ProductUrl) ? "" : $"\n\nTry it: {request.ProductUrl}";
        var locationLine = LocationSentence(request.Location);
        var learningLine = request.CompanyLearning?.HasData == true
            ? $"\n\nPrevious learning: {request.CompanyLearning.PreferredPostStyle}; the main CTA should be '{request.CompanyLearning.PreferredCta}'."
            : string.Empty;
        var proofLine = variation > 0
            ? $"\n\nVariation {variation}: open with the pain, show proof or an example, and close with one small action."
            : string.Empty;

        return profile.Name switch
        {
            "LinkedIn" =>
                $"{request.TargetAudience} do not need more manual work. They need a repeatable way to reach {request.CampaignGoal.ToLowerInvariant()}.{locationLine}\n\n{request.ProductName} focuses on {request.ValueProposition}.\n\nToday's angle: {TitleForAngle(angle)}.{proofLine}{learningLine}{urlLine}",
            "TikTok" =>
                $"Open with the problem in the first 2 seconds, show {request.ProductName}, and make the result concrete for {request.Location.Summary}: {request.ValueProposition}.{proofLine}{learningLine}{urlLine}",
            "Instagram" =>
                $"Turn this into a {profile.Format}: problem, product moment, result and CTA. {request.ProductName} helps {request.TargetAudience} generate {request.CampaignGoal.ToLowerInvariant()} in {request.Location.Summary}.{proofLine}{learningLine}{urlLine}",
            "Facebook" =>
                $"{request.TargetAudience}{LocationPhrase(request.Location)} often know the problem but delay the solution. Position {request.ProductName} as the practical next step: {request.ValueProposition}.{proofLine}{learningLine}{urlLine}",
            _ =>
                $"{request.ProductName} helps {request.TargetAudience} in {request.Location.Summary} with {request.ValueProposition}. Focus this post on {TitleForAngle(angle)}.{proofLine}{learningLine}{urlLine}"
        };
    }

    private static string CreativeBriefFor(PlatformProfile profile, AiMarketingPlanRequest request, string angle, int variation)
    {
        var learning = request.CompanyLearning?.HasData == true
            ? $" Reuse what converted before: {request.CompanyLearning.RecommendedCampaignBrief}"
            : string.Empty;
        var variant = variation > 0 ? $" Variation {variation}: change hook and proof point without changing the campaign offer." : string.Empty;
        return $"Format: {profile.Format}. Style: {profile.Style}. Show product context, the audience problem in {request.Location.Summary}, and one measurable next step. Angle: {TitleForAngle(angle)}. Tone: {request.Tone}.{variant}{learning}";
    }

    private static string HashtagsFor(PlatformProfile profile, AiMarketingPlanRequest request, int variation)
    {
        if (variation <= 0)
            return profile.Hashtags;

        var goalTag = request.CampaignGoal
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "growth";
        return $"{profile.Hashtags} #{Slugify(goalTag)} #{Slugify(request.ProductName)}";
    }

    private static string VariationSuffix(int variation)
    {
        return variation <= 0 ? string.Empty : $" v{variation}";
    }

    private static string CallToActionFor(AiMarketingPlanRequest request)
    {
        if (request.CompanyLearning?.HasData == true && !string.IsNullOrWhiteSpace(request.CompanyLearning.PreferredCta))
            return request.CompanyLearning.PreferredCta;

        return string.IsNullOrWhiteSpace(request.ProductUrl)
            ? $"Ask for a demo of {request.ProductName}"
            : $"Visit {request.ProductUrl}";
    }

    private static string SubjectFor(AiMarketingPlanRequest request, string theme)
    {
        return theme switch
        {
            "Quick intro" => $"{request.ProductName} for {request.TargetAudience}",
            "Pain and cost" => $"Is this slowing down your {request.CampaignGoal.ToLowerInvariant()}?",
            "Use case" => $"A practical workflow using {request.ProductName}",
            "Proof and trust" => $"Why teams choose {request.ProductName}",
            "Offer" => $"A simpler way to start with {request.ProductName}",
            _ => $"Should I close the loop on {request.ProductName}?"
        };
    }

    private static string EmailBodyFor(AiMarketingPlanRequest request, string theme, bool learnedShortEmail)
    {
        var cta = string.IsNullOrWhiteSpace(request.ProductUrl)
            ? "Would it make sense to send you a quick walkthrough?"
            : $"You can review it here: {request.ProductUrl}";

        if (learnedShortEmail)
        {
            return $"Hi {{FirstName}},\n\nBased on the results we are seeing, the clearest next step is simple: {request.CompanyLearning?.PreferredCta ?? CallToActionFor(request)}.\n\n{request.ProductName} helps {request.TargetAudience} with {request.ValueProposition}.\n\n{cta}";
        }

        return theme switch
        {
            "Quick intro" =>
                $"Hi {{FirstName}},\n\nI wanted to introduce {request.ProductName}.\n\n{request.CompanyOrIdea}\n\nFor {request.TargetAudience} in {request.Location.Summary}, the main value is: {request.ValueProposition}.\n\n{cta}",
            "Pain and cost" =>
                $"Hi {{FirstName}},\n\nThe expensive part is not just the task itself. It is the delay, manual follow-up and missed opportunities around it.\n\nThat is where {request.ProductName} can help: {request.ValueProposition}.\n\n{cta}",
            "Use case" =>
                $"Hi {{FirstName}},\n\nOne practical use case: take a recurring acquisition workflow, define the audience and region, and let {request.ProductName} keep the next action moving.\n\nFor {request.TargetAudience} in {request.Location.Summary}, that means a clearer path to {request.CampaignGoal.ToLowerInvariant()}.\n\n{cta}",
            "Proof and trust" =>
                $"Hi {{FirstName}},\n\nThe goal is not more tools. It is a repeatable process that can be measured.\n\n{request.ProductName} keeps the focus on {request.CampaignGoal.ToLowerInvariant()}, with messaging built around {request.ValueProposition}.\n\n{cta}",
            "Offer" =>
                $"Hi {{FirstName}},\n\nIf this is relevant, the easiest next step is to test {request.ProductName} with one specific campaign or workflow.\n\nYou will quickly see whether {request.ValueProposition} fits your team.\n\n{cta}",
            _ =>
                $"Hi {{FirstName}},\n\nI do not want to keep chasing if this is not a priority.\n\nShould I close the loop, or is {request.ProductName} worth a quick look for {request.TargetAudience}?\n\n{cta}"
        };
    }

    private static int DefaultHourFor(string platform)
    {
        return platform switch
        {
            "LinkedIn" => 8,
            "TikTok" => 18,
            "Instagram" => 17,
            "Facebook" => 12,
            "YouTube Shorts" => 19,
            _ => 10
        };
    }

    private static int CountAudienceContacts(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
            return 0;

        return audience
            .Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(contact => !string.IsNullOrWhiteSpace(contact));
    }

    private static int ScaleReach(int reach, AiAudienceLocation location)
    {
        var multiplier = location.Scope switch
        {
            "City" => 0.58m,
            "Country" => 0.82m,
            _ => 1.0m
        };

        return Math.Max(1, (int)Math.Round(reach * multiplier));
    }

    private static string LocationPhrase(AiAudienceLocation location)
    {
        return location.Scope.Equals("World", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" in {location.Summary}";
    }

    private static string LocationSentence(AiAudienceLocation location)
    {
        return location.Scope.Equals("World", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" Focus the message on {location.Summary}.";
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }

    private static string Slugify(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join("-", new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "campaign" : Clip(slug, 40);
    }

    private sealed record PlatformProfile(
        string Name,
        string Format,
        string Style,
        string Hashtags,
        int BaseReach,
        decimal InteractionRate,
        decimal ConversionRate);
}
