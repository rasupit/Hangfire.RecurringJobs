namespace Hangfire.Extension.Web.Options;

public sealed class HangfireExtensionOptions
{
    public const string DefaultAuthorizationPolicy = "RecurringJobAdmin";

    private string routePrefix = "/recurring-jobs";

    public string RoutePrefix
    {
        get => routePrefix;
        set => routePrefix = NormalizeRoutePrefix(value);
    }

    public bool RequireAuthorization { get; set; } = true;

    public string AuthorizationPolicy { get; set; } = DefaultAuthorizationPolicy;

    public string RoutePrefixNormalized => NormalizeRoutePrefix(RoutePrefix);

    public string ApiRoutePrefix => $"{RoutePrefixNormalized}/api/jobs/recurring";

    public void Validate()
    {
        _ = RoutePrefixNormalized;

        if (RequireAuthorization && string.IsNullOrWhiteSpace(AuthorizationPolicy))
        {
            throw new InvalidOperationException(
                "Hangfire.Extension requires a non-empty authorization policy name when RequireAuthorization is enabled.");
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
}
