using Bunit;
using FluentAssertions;
using Frontend.Components;

namespace App.Tests.Components;

public class PagerTests : TestContext
{
    [Fact]
    public void ShouldRenderNothing_ForSinglePage()
    {
        var cut = RenderComponent<Pager>(p => p
            .Add(x => x.BasePath, "products")
            .Add(x => x.PageNumber, 1)
            .Add(x => x.TotalPages, 1));

        cut.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void ShouldLinkEveryPage_AndMarkTheCurrentOne()
    {
        var cut = RenderComponent<Pager>(p => p
            .Add(x => x.BasePath, "products")
            .Add(x => x.PageNumber, 2)
            .Add(x => x.TotalPages, 3));

        cut.FindAll(".pager-pages a").Select(a => a.GetAttribute("href"))
            .Should().Equal("products", "products?page=2", "products?page=3");
        cut.Find("[aria-current='page']").TextContent.Should().Be("2");
    }
}
