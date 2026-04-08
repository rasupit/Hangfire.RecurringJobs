using Hangfire.Extension.Services;

namespace Hangfire.Extension.Tests;

public sealed class CronExpressionPreviewServiceTests
{
    private readonly CronExpressionPreviewService service = new();

    [Fact]
    public void CreatePreview_ReturnsDescriptionAndUpcomingOccurrences_ForValidCron()
    {
        var preview = service.CreatePreview(
            "0 2 * * *",
            TimeZoneInfo.Utc,
            new DateTimeOffset(2026, 4, 8, 0, 0, 0, TimeSpan.Zero));

        Assert.True(preview.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(preview.Description));
        Assert.NotEmpty(preview.UpcomingOccurrences);
    }

    [Fact]
    public void CreatePreview_AllowsManualTriggerOnlySchedules()
    {
        var preview = service.CreatePreview(
            "0 0 31 2 *",
            TimeZoneInfo.Utc,
            new DateTimeOffset(2026, 4, 8, 0, 0, 0, TimeSpan.Zero));

        Assert.True(preview.IsValid);
        Assert.Empty(preview.UpcomingOccurrences);
        Assert.Contains("manual-trigger only", preview.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
