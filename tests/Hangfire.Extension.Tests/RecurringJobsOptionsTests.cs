using Hangfire.Extension;

namespace Hangfire.Extension.Tests;

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
}
