namespace Hangfire.RecurringJobs;

public sealed class RecurringJobsOptions
{
    public const string DefaultAuthorizationPolicy = "RecurringJobAdmin";

    private string routePrefix = "/recurring-jobs";

    public string RoutePrefix
    {
        get => routePrefix;
        set => routePrefix = NormalizeRoutePrefix(value);
    }

    public bool RequireAuthorization { get; set; } = true;

    public TimeZoneInfo DefaultTimeZone { get; set; } = TimeZoneInfo.Utc;

    public bool AutoRegisterOnStartup { get; set; } = false;

    public string AuthorizationPolicy { get; set; } = DefaultAuthorizationPolicy;

    public IList<string> Styles { get; } = [];

    public string RoutePrefixNormalized => NormalizeRoutePrefix(RoutePrefix);

    public string ApiRoutePrefix => $"{RoutePrefixNormalized}/api/jobs/recurring";

    public bool UseEmbeddedStyles => NormalizedStyles.Count == 0;

    public IReadOnlyList<string> NormalizedStyles => Styles
        .Select(NormalizeStyle)
        .Where(static style => style is not null)
        .Cast<string>()
        .ToArray();

    public void Validate()
    {
        _ = RoutePrefixNormalized;
        _ = NormalizedStyles;

        if (RequireAuthorization && string.IsNullOrWhiteSpace(AuthorizationPolicy))
        {
            throw new InvalidOperationException(
                "Hangfire.RecurringJobs requires a non-empty authorization policy name when RequireAuthorization is enabled.");
        }
    }

    public static string NormalizeRoutePrefix(string? routePrefix)
    {
        if (string.IsNullOrWhiteSpace(routePrefix))
        {
            throw new ArgumentException("A route prefix is required.", nameof(routePrefix));
        }

        var normalized = routePrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    public static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        return style.Trim();
    }
}
