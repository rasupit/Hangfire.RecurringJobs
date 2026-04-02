using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobPageTests
{
    [Fact]
    public void TotalPages_IsCalculatedFromTotalCountAndPageSize()
    {
        var page = new RecurringJobPage(
            Items: [],
            Page: 2,
            PageSize: 25,
            TotalCount: 51);

        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public void TotalPages_IsZero_WhenThereAreNoItems()
    {
        var page = new RecurringJobPage(
            Items: [],
            Page: 1,
            PageSize: 25,
            TotalCount: 0);

        Assert.Equal(0, page.TotalPages);
    }
}
