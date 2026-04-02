using Hangfire.Common;

namespace Hangfire.Extension.Core.Models;

public sealed record RecurringJobDefinition(
    string Id,
    Job Job,
    string CronExpression,
    TimeZoneInfo TimeZone,
    string Queue);
