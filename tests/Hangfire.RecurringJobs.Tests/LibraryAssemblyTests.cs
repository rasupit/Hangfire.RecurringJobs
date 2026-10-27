namespace Hangfire.RecurringJobs.Tests;

public sealed class LibraryAssemblyTests
{
    // A Program type in the library clashes with the host's own Program in WebApplicationFactory<Program> tests.
    [Fact]
    public void LibraryAssembly_DoesNotContainProgramType()
    {
        var assembly = typeof(RecurringJobsOptions).Assembly;

        Assert.DoesNotContain(assembly.GetTypes(), type => type.Name == "Program");
        Assert.Null(assembly.EntryPoint);
    }
}
