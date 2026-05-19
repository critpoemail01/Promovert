using Promovert.Pages.App.Planner;

namespace Promovert.Tests;

public class PlannerDefaultsTests
{
    [Fact]
    public void PlannerInput_DefaultsToNextWeekCampaign()
    {
        var input = new IndexModel.PlannerInput();
        var today = DateOnly.FromDateTime(DateTime.Today);

        Assert.Equal(DayOfWeek.Monday, input.StartDate.DayOfWeek);
        Assert.True(input.StartDate > today);
        Assert.True(input.StartDate <= today.AddDays(7));
        Assert.Equal(7, input.DurationDays);
        Assert.Equal("Daily", input.Frequency);
    }
}
