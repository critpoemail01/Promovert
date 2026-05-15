using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Promovert.Services;

public sealed class CampaignLibraryService : ICampaignLibraryService
{
    private const string GeneralSectorKey = "general-growth";

    private static readonly IReadOnlyList<SectorDefinition> Sectors =
    [
        new(GeneralSectorKey, "General / growth", [],
        [
            Template("general-lead-capture", "Lead capture campaign", "qualified leads", "diagnosis, proposal or initial contact", "potential clients interested in the product or service", ["LinkedIn", "Instagram", "Facebook", "Email"], 14, "ThreePerWeek", "Explain the pain, present the promise and convert visitors into a commercial conversation.", "Educational content, short proof, objections and a direct CTA.", "Landing page with promise, benefits, proof and a simple form.", "Name, email, company, need and preferred contact.", "Context email, proof, objections and invitation to talk.", "leads, conversion rate, channel that generated the most contacts and next action"),
            Template("general-launch", "Launch campaign", "first customers or subscribers", "limited-time launch offer", "early audience most likely to try the solution", ["Instagram", "TikTok", "LinkedIn", "Email"], 21, "Daily", "Create awareness, explain the offer and give a clear reason to act now.", "Problem, novelty, benefit, proof and light urgency.", "Launch page with offer, CTA and channel tracking.", "Name, email, segment and main interest.", "Launch sequence, social proof and final call.", "reach, clicks, leads, subscriptions/purchases and best message"),
            Template("general-proof", "Social proof campaign", "more confident leads", "case, testimonial or demonstrable result", "customers who need trust before buying or subscribing", ["LinkedIn", "Facebook", "Instagram", "Email"], 30, "Weekly", "Use results, examples and testimonials to reduce perceived risk.", "Before/after, customer story, numbers and CTA for the next step.", "Landing page with case, result, FAQ and form.", "Name, contact, company/need and context.", "Case delivery, insight follow-up and proposal invitation.", "leads influenced by social proof, landing conversion and best channel")
        ]),

        new("clinics", "Clinics and healthcare", ["clinic", "doctor", "dentist", "medical", "health", "consultation", "check-up"],
        [
            Template("clinics-first-visit", "First appointment campaign", "first appointments booked", "first appointment with initial assessment", "local people who need to book an appointment", ["Instagram", "Facebook", "TikTok", "Email"], 14, "Weekdays", "Educate around symptoms, reduce booking friction and close with local proof.", "Before/after, clinical team, appointment benefits and booking CTA.", "Page with specialty, trust, opening hours and booking CTA.", "Name, email, phone, intended specialty and time preference.", "Confirmation email, booking reminder and availability follow-up.", "appointment requests, submitted forms and conversion rate by channel"),
            Template("clinics-checkup", "Check-up campaign", "check-up requests", "preventive check-up with limited availability", "families, busy professionals and recurring patients", ["Facebook", "Instagram", "Email"], 21, "ThreePerWeek", "Create light urgency around prevention and easy booking.", "Short educational content and reassurance proof.", "Page with check-up package, inclusions and form.", "Name, contact, approximate age and best time.", "Education sequence, objections and final call.", "check-up requests, cost per lead and request source"),
            Template("clinics-online-booking", "Online booking campaign", "online bookings", "fast booking without phone calls", "patients who prefer booking online", ["Instagram", "Facebook", "Google", "Email"], 14, "Daily", "Show booking simplicity and reduce missed calls.", "Three-step process demonstration.", "Landing page focused on the booking button.", "Name, email, phone and specialty.", "Confirmation, reminder and abandonment recovery.", "booking clicks, forms and confirmed reservations")
        ]),

        new("construction", "Construction and renovation", ["construction", "renovation", "remodel", "architecture", "builder", "quote"],
        [
            Template("construction-quote", "Quote request campaign", "qualified quote requests", "free quote for construction or renovation", "homeowners, companies and space managers", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Qualify interested people by project type and location.", "Project photos, process, checklist and quote CTA.", "Page with project types, proof, served areas and form.", "Name, contact, project location, project type and timing.", "Triage email, detail request and visit scheduling.", "quote requests, scheduled visits and source by channel"),
            Template("construction-projects", "Completed projects campaign", "leads with social proof", "portfolio of recent projects with proposal contact", "customers who need trust before requesting a proposal", ["Instagram", "Facebook", "LinkedIn"], 30, "Weekdays", "Use real projects to build trust and generate requests.", "Before/after, behind the scenes and final results.", "Gallery/landing page with cases and CTA to assess a project.", "Name, email, phone and project type.", "Sequence with similar cases and invitation for a technical visit.", "landing views, leads and highest-converting cases"),
            Template("construction-remodel", "Renovation campaign", "renovation requests", "initial renovation assessment", "homeowners, shops and offices", ["Instagram", "Facebook", "TikTok", "Email"], 30, "ThreePerWeek", "Segment by problem: kitchen, bathroom, shop or office.", "Visual transformations and common mistakes to avoid.", "Page with renovation types and assessment form.", "Name, contact, location, room and estimated budget.", "Follow-up by renovation type and relevant proof.", "requests by segment, interactions and quotes started")
        ]),

        new("industrial-operations", "Industry and operations", ["industrial", "industry", "manufacturing", "factory", "production", "operations", "maintenance", "energy", "planning", "quality"],
        [
            Template("industrial-efficiency", "Operational efficiency campaign", "operational improvement leads", "free efficiency assessment or process diagnosis", "operations, production, maintenance and industrial managers", ["LinkedIn", "Email", "Facebook"], 21, "ThreePerWeek", "Connect production, energy, maintenance or planning problems to measurable gains.", "Operational pain, invisible cost, checklist and improvement example.", "Landing page with diagnosis, served sectors and qualified form.", "Name, professional email, company, role, area and main bottleneck.", "Checklist email, similar case and diagnosis invitation.", "diagnosis requests, leads by sector and qualified opportunities"),
            Template("industrial-maintenance", "Maintenance downtime campaign", "maintenance contact requests", "plan to reduce downtime and delays", "maintenance, production and industrial planning leads", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Address the impact of downtime, rework and scattered information.", "Risk signals, preventive routines, mini case and conversation CTA.", "Page focused on maintenance, availability and financial impact.", "Name, company, role, operation type and current problem.", "Educational sequence, proof and process assessment invitation.", "maintenance leads, decision-maker clicks and conversion rate"),
            Template("industrial-planning", "Production planning campaign", "planning meetings", "planning and cost improvement session", "industrial SMEs, factories and teams with manual planning processes", ["LinkedIn", "Email", "YouTube Shorts"], 30, "ThreePerWeek", "Show how lack of visibility affects deadlines, energy, costs and decisions.", "Comparisons, common errors, before/after and session CTA.", "Landing page with planning challenges and qualification form.", "Name, email, company, operation size and current tool.", "Diagnosis email, ROI, use case and booking.", "booked meetings, qualified leads and highest-intent channel")
        ]),

        new("b2b-software", "B2B software", ["software", "saas", "b2b", "crm", "erp", "dashboard", "workflow", "automation", "excel", "system", "platform"],
        [
            Template("b2b-demo", "Demo campaign", "booked demos", "free demo oriented to the customer's process", "directors, operations managers and B2B decision makers", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Turn operational pain into demo requests with concrete proof.", "Problem, workflow, measurable result and demo CTA.", "Page with promise, screenshots, form and calendar.", "Name, professional email, company, role and main challenge.", "Context email, proof, objections and demo invitation.", "demo requests, companies reached and conversion by channel"),
            Template("b2b-excel", "Replace Excel with software campaign", "leads using manual processes", "free diagnosis to replace Excel/separate processes", "teams still running operations in spreadsheets or scattered systems", ["LinkedIn", "Email", "TikTok", "YouTube Shorts"], 21, "ThreePerWeek", "Attack the Excel pain and show the hidden cost of manual operations.", "Comparisons, operational chaos signals and mini demo.", "Landing page with maturity checklist and form.", "Name, email, company, area and process done in Excel.", "Diagnosis sequence, use case, ROI and demo invitation.", "diagnosis requests, interactions and demos converted"),
            Template("b2b-case-study", "Case study campaign", "proof-qualified leads", "case study applicable to the customer's sector", "decision makers who need proof before talking to sales", ["LinkedIn", "Email", "Facebook"], 30, "Weekly", "Use proof and result narrative for more mature leads.", "Context, problem, solution, metrics and CTA.", "Landing page with case, result and access form.", "Name, business email, company and interest.", "Case delivery, insight follow-up and conversation invitation.", "downloads/accesses, qualified leads and demo requests")
        ]),

        new("restaurants", "Restaurants", ["restaurant", "menu", "reservation", "food", "bar", "cafe", "hospitality"],
        [
            Template("restaurants-weekly-menu", "Weekly menu campaign", "reservations and visits", "featured weekly menu", "local customers and visitors nearby", ["Instagram", "Facebook", "TikTok", "Email"], 7, "Daily", "Generate visits with specific dishes and weekly moments.", "Dish of the day, behind the scenes, team and reviews.", "Simple page with menu, hours and reservation.", "Name, contact, intended date and number of people.", "Menu reminder and reservation invitation.", "reservations, menu clicks and interactions by dish"),
            Template("restaurants-reservations", "Reservation campaign", "online reservations", "table reserved for lunch/dinner", "people looking for a restaurant nearby", ["Instagram", "Facebook", "Google", "Email"], 14, "Weekdays", "Reduce reservation friction and promote available slots.", "Atmosphere, signature dishes and reservation CTA.", "Landing page with reservation and highlights.", "Name, contact, date, time and number of people.", "Confirmation, reminder and menu upsell.", "reservations, leads and channels with highest conversion"),
            Template("restaurants-private-events", "Private events campaign", "event requests", "proposal for birthdays, companies or groups", "companies, groups and event organizers", ["Instagram", "Facebook", "LinkedIn", "Email"], 30, "ThreePerWeek", "Position the restaurant as a solution for small and medium events.", "Space, group menus, proof and offer.", "Page with event types and form.", "Name, contact, event type, date and number of people.", "Email with menu options and detail request.", "event requests, estimated value and source")
        ]),

        new("real-estate", "Real estate", ["real estate", "property", "house", "apartment", "valuation", "premium"],
        [
            Template("realestate-valuation", "Free valuation campaign", "valuation requests", "free property valuation", "owners considering selling or renting", ["Facebook", "Instagram", "LinkedIn", "Email"], 21, "ThreePerWeek", "Capture owners before they place the property on the market.", "Market value, selling mistakes and local proof.", "Page with valuation promise and form.", "Name, contact, location, property type and objective.", "Confirmation email, checklist and valuation invitation.", "valuation requests, in-demand areas and qualified leads"),
            Template("realestate-capture", "Property acquisition campaign", "new properties for the portfolio", "sales plan for owners", "property owners intending to sell", ["Facebook", "Instagram", "Email"], 30, "Weekdays", "Show the sales process and reduce owner uncertainty.", "Sales proof, promotion plan and next steps.", "Landing page with acquisition plan and form.", "Name, contact, area, property type and timing.", "Proof sequence, process and booking.", "captured owners, forms and booked meetings"),
            Template("realestate-premium", "Premium properties campaign", "premium buyer leads", "early access to premium properties", "buyers with purchasing power and investors", ["Instagram", "LinkedIn", "Facebook", "Email"], 21, "ThreePerWeek", "Create exclusivity and capture qualified buyers.", "Lifestyle, property details, area and scarcity.", "Page with property/collection and interest form.", "Name, contact, budget, area and objective.", "Email with selected properties and visit invitation.", "premium leads, visits and conversion by property")
        ]),

        new("ecommerce", "Ecommerce", ["shop", "store", "ecommerce", "checkout", "cart", "product", "buy", "shipping"],
        [
            Template("ecommerce-product-launch", "Product launch campaign", "initial purchases", "limited-time launch offer", "buyers interested in the product and brand followers", ["Instagram", "TikTok", "Facebook", "Email"], 14, "Daily", "Combine visual discovery, proof and urgency.", "Unboxing, benefits, social proof and offer.", "Product page with form/checkout and UTMs.", "Email, interest, product preference.", "Launch sequence, proof and final call.", "sales, leads, recovered carts and estimated ROAS"),
            Template("ecommerce-abandoned-cart", "Abandoned cart campaign", "recovered carts", "incentive to complete purchase", "visitors who showed interest", ["Email", "Facebook", "Instagram"], 7, "Daily", "Remove doubts and recover buying intent.", "Benefits, objections, reviews and incentive.", "Page with offer and proof.", "Email and product of interest.", "Email 1 reminder, email 2 proof, email 3 urgency.", "recovered carts and attributed revenue")
        ]),

        new("education", "Education and courses", ["course", "learn", "training", "school", "academy", "class", "student", "education"],
        [
            Template("education-enrolment", "Enrollment campaign", "course or training enrollments", "open class or free session", "professionals, students or teams who want to learn a skill", ["LinkedIn", "Instagram", "YouTube Shorts", "Email"], 21, "ThreePerWeek", "Show the practical result of learning and reduce doubts before enrollment.", "Short class, progress, student examples and enrollment CTA.", "Page with program, result, teacher and form.", "Name, email, learning goal and current level.", "Useful content email, proof and open class invitation.", "enrollments, leads, conversion rate and most interesting module"),
            Template("education-webinar", "Webinar campaign", "webinar registrations", "free webinar on a specific topic", "audience that needs to solve a concrete doubt before buying", ["LinkedIn", "Email", "Instagram"], 14, "Weekdays", "Use an educational event to capture leads and prepare a later sale.", "Problem, webinar promise, agenda and expert proof.", "Registration landing page with calendar and reminders.", "Name, email, company/optional and main question.", "Confirmation, reminders, replay and final offer.", "registrations, attendance, questions and sales/subscriptions after the webinar")
        ]),

        new("professional-services", "Professional services", ["consulting", "agency", "law", "legal", "accounting", "service", "services", "audit"],
        [
            Template("services-assessment", "Initial assessment campaign", "assessment requests", "free initial assessment or triage", "companies or customers who need to choose a trusted provider", ["LinkedIn", "Facebook", "Instagram", "Email"], 14, "ThreePerWeek", "Build trust with diagnosis, process and experience proof.", "Common mistakes, checklist, proof and assessment CTA.", "Landing page with services, method, cases and form.", "Name, email, company, need and urgency.", "Triage email, proof, objections and meeting invitation.", "assessment requests, meeting rate and best channel"),
            Template("services-retainer", "Recurring contract campaign", "recurring service leads", "monthly plan or continuous support", "customers who need regular and predictable support", ["LinkedIn", "Email", "Facebook"], 30, "Weekly", "Show the cost of solving everything ad hoc versus continuous support.", "Comparison, process, cases and recurring benefits.", "Page with packages, deliverables and qualification form.", "Name, contact, business, need and size.", "Educational sequence, value proposition and diagnosis call.", "recurring leads, meetings and potential contract value")
        ])
    ];

    public IReadOnlyList<SectorCampaignRecommendation> Recommend(CampaignLibraryRequest request, int maxResults = 3)
    {
        var text = NormalizeForSearch(string.Join(" ", new[]
        {
            request.ProductName,
            request.CompanyOrIdea,
            request.TargetAudience,
            request.ValueProposition,
            request.CampaignGoal,
            request.DetectedApplicationType
        }));

        var ranked = Sectors
            .Where(sector => !sector.Key.Equals(GeneralSectorKey, StringComparison.OrdinalIgnoreCase))
            .Select(sector => new
            {
                Sector = sector,
                Score = sector.Keywords.Count(keyword => KeywordMatches(text, keyword))
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Sector.Name)
            .ToList();

        var selectedSectors = ranked
            .Where(x => x.Score > 0)
            .Select(x => x.Sector)
            .ToList();

        var general = Sectors.First(x => x.Key == GeneralSectorKey);
        if (selectedSectors.Count == 0)
            selectedSectors.Add(general);
        else
            selectedSectors.Add(general);

        return PickTemplates(selectedSectors, Math.Clamp(maxResults, 1, 8));
    }

    public SectorCampaignRecommendation? Find(string key)
    {
        return Sectors
            .SelectMany(x => x.Templates)
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<SectorCampaignRecommendation> PickTemplates(
        IReadOnlyList<SectorDefinition> sectors,
        int maxResults)
    {
        var recommendations = new List<SectorCampaignRecommendation>();
        var templateIndex = 0;

        while (recommendations.Count < maxResults)
        {
            var added = false;
            foreach (var sector in sectors)
            {
                if (templateIndex >= sector.Templates.Count)
                    continue;

                var template = sector.Templates[templateIndex];
                if (recommendations.Any(x => x.Key.Equals(template.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                recommendations.Add(template);
                added = true;
                if (recommendations.Count == maxResults)
                    break;
            }

            if (!added)
                break;

            templateIndex++;
        }

        return recommendations;
    }

    private static SectorCampaignRecommendation Template(
        string key,
        string title,
        string goal,
        string offer,
        string audience,
        IReadOnlyList<string> platforms,
        int durationDays,
        string frequency,
        string strategy,
        string creativeAngle,
        string landingPageBrief,
        string formBrief,
        string followUpBrief,
        string reportFocus)
    {
        return new SectorCampaignRecommendation(
            key,
            string.Empty,
            title,
            goal,
            offer,
            audience,
            platforms,
            durationDays,
            frequency,
            strategy,
            creativeAngle,
            landingPageBrief,
            formBrief,
            followUpBrief,
            reportFocus);
    }

    private static bool KeywordMatches(string text, string keyword)
    {
        var normalizedKeyword = NormalizeForSearch(keyword);
        return normalizedKeyword.Length <= 3
            ? Regex.IsMatch(text, $@"\b{Regex.Escape(normalizedKeyword)}\b", RegexOptions.IgnoreCase)
            : text.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record SectorDefinition(string Key, string Name, IReadOnlyList<string> Keywords, IReadOnlyList<SectorCampaignRecommendation> RawTemplates)
    {
        public IReadOnlyList<SectorCampaignRecommendation> Templates { get; } = RawTemplates
            .Select(template => template with { Sector = Name })
            .ToList();
    }
}
