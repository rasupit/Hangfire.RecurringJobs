using Hangfire.Extension.Web.Services;

namespace Hangfire.Extension.Tests;

public sealed class CronExpressionValidatorTests
{
    [Fact]
    public void IsValid_ReturnsTrue_ForValidCron()
    {
        var validator = new CronExpressionValidator();

        var isValid = validator.IsValid("0 12 * * *", out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidCron()
    {
        var validator = new CronExpressionValidator();

        var isValid = validator.IsValid("not-a-cron", out var error);

        Assert.False(isValid);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
