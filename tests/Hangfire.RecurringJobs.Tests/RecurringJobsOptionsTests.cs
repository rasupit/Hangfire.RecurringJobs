using Hangfire.RecurringJobs;

namespace Hangfire.RecurringJobs.Tests;

public sealed class RecurringJobsOptionsTests
{
    [Fact]
    public void Validate_Throws_WhenAuthorizationIsRequiredButPolicyIsMissing()
    {
        var options = new RecurringJobsOptions
        {
            RequireAuthorization = true,
            AuthorizationPolicy = " "
        };

        var action = () => options.Validate();

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenAuthorizationIsDisabled()
    {
        var options = new RecurringJobsOptions
        {
            RequireAuthorization = false,
            AuthorizationPolicy = string.Empty
        };

        var exception = Record.Exception(() => options.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void NormalizeRoutePrefix_AddsLeadingSlash_AndTrimsTrailingSlash()
    {
        var normalized = RecurringJobsOptions.NormalizeRoutePrefix("recurring-jobs/");

        Assert.Equal("/recurring-jobs", normalized);
    }

    [Fact]
    public void StylesPath_UsesLibraryDefault_WhenStylesAreNotConfigured()
    {
        var options = new RecurringJobsOptions();

        Assert.True(options.UseEmbeddedStyles);
        Assert.Empty(options.NormalizedStyles);
    }

    [Fact]
    public void NormalizeStyle_ReturnsNull_ForBlankValues()
    {
        var normalized = RecurringJobsOptions.NormalizeStyle(" ");

        Assert.Null(normalized);
    }

    [Fact]
    public void NormalizedStyles_FiltersBlankValues_AndTrimsEntries()
    {
        var options = new RecurringJobsOptions();
        options.Styles.Add(" /lib/bootstrap/dist/css/bootstrap.min.css ");
        options.Styles.Add(" ");
        options.Styles.Add("/css/site.css");

        Assert.Equal(
            ["/lib/bootstrap/dist/css/bootstrap.min.css", "/css/site.css"],
            options.NormalizedStyles);
    }
}
