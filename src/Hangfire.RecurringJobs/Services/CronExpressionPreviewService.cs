using CronExpressionDescriptor;
using Cronos;
using Hangfire.RecurringJobs.Models;

namespace Hangfire.RecurringJobs.Services;

public sealed class CronExpressionPreviewService
{
    public RecurringJobCronPreview CreatePreview(
        string? cronExpression,
        TimeZoneInfo? timeZone = null,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return new RecurringJobCronPreview(
                IsValid: false,
                Summary: "Cron expression is required.",
                Description: null,
                UpcomingOccurrences: []);
        }

        try
        {
            var schedule = CronExpression.Parse(cronExpression, CronFormat.Standard);
            var description = ExpressionDescriptor.GetDescription(cronExpression);
            var effectiveTimeZone = timeZone ?? TimeZoneInfo.Utc;
            var cursor = now ?? DateTimeOffset.Now;
            var upcomingOccurrences = new List<string>(capacity: 3);

            for (var index = 0; index < 3; index++)
            {
                var nextOccurrence = schedule.GetNextOccurrence(cursor, effectiveTimeZone);
                if (nextOccurrence is null)
                {
                    break;
                }

                upcomingOccurrences.Add(TimeZoneInfo.ConvertTime(nextOccurrence.Value, effectiveTimeZone).ToString("yyyy-MM-dd HH:mm zzz"));
                cursor = nextOccurrence.Value;
            }

            if (upcomingOccurrences.Count == 0)
            {
                return new RecurringJobCronPreview(
                    IsValid: true,
                    Summary: "This schedule is valid, but it has no future occurrence, so it will stay manual-trigger only.",
                    Description: description,
                    UpcomingOccurrences: []);
            }

            return new RecurringJobCronPreview(
                IsValid: true,
                Summary: $"Next run: {upcomingOccurrences[0]}",
                Description: description,
                UpcomingOccurrences: upcomingOccurrences);
        }
        catch (CronFormatException exception)
        {
            return new RecurringJobCronPreview(
                IsValid: false,
                Summary: exception.Message,
                Description: null,
                UpcomingOccurrences: []);
        }
    }
}
