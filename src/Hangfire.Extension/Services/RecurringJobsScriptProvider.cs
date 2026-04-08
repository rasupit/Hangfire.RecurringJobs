using System.Reflection;

namespace Hangfire.Extension.Services;

internal static class RecurringJobsScriptProvider
{
    private static readonly Lazy<string> CachedScript = new(LoadScript);

    public static string GetScript()
        => CachedScript.Value;

    private static string LoadScript()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Hangfire.Extension.RecurringJobs.Script.js")
            ?? throw new InvalidOperationException("Could not load the embedded recurring jobs script.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
