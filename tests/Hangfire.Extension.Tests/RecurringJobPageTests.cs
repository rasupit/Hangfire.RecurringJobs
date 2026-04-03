using Hangfire.Extension.Models;

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

    [Fact]
    public void NavigationFlags_AreCalculatedFromPageAndTotalPages()
    {
        var page = new RecurringJobPage(
            Items: [],
            Page: 2,
            PageSize: 25,
            TotalCount: 60,
            Search: "report");

        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal("report", page.Search);
    }
}
