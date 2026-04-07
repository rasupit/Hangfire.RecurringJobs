using System.Reflection;

namespace Hangfire.Extension.Services;

internal static class RecurringJobsStyleProvider
{
    private static readonly Lazy<string> CachedStyles = new(LoadStyles);

    public static string GetStyles()
        => CachedStyles.Value;

    private static string LoadStyles()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Hangfire.Extension.RecurringJobs.Styles.css")
            ?? throw new InvalidOperationException("Could not load the embedded recurring jobs stylesheet.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
