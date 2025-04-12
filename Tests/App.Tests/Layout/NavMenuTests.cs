using Bunit;
using Frontend.Layout;

namespace App.Tests.Layout;

public class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_ShouldRender_WithCorrectLinks()
    {
        // Arrange & Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        // Instead of checking the exact HTML, let's check for the presence of key elements
        // This is more resilient to minor changes in the rendered HTML

        // Check for the navbar brand
        cut.Find("a.navbar-brand").MarkupMatches("<a class=\"navbar-brand\" href=\"\">NATS Shop</a>");

        // Check for the navigation links
        var navLinks = cut.FindAll("a.nav-link");
        Assert.Equal(4, navLinks.Count);

        // Check the Home link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Home"));

        // Check the About link
        Assert.Contains(navLinks, link => link.TextContent.Contains("About"));

        // Check the Admin link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Admin"));

        // Check the Products link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Products"));

    }

    [Fact]
    public void NavMenu_ShouldToggle_WhenButtonClicked()
    {
        // Arrange
        var cut = RenderComponent<NavMenu>();

        // Initial state should be collapsed
        Assert.Contains("collapse", cut.Find("div.nav-scrollable").ClassList);

        // Act - click the toggle button
        cut.Find("button.navbar-toggler").Click();

        // Assert - menu should be expanded (no collapse class)
        Assert.DoesNotContain("collapse", cut.Find("div.nav-scrollable").ClassList);

        // Act - click the toggle button again
        cut.Find("button.navbar-toggler").Click();

        // Assert - menu should be collapsed again
        Assert.Contains("collapse", cut.Find("div.nav-scrollable").ClassList);
    }
}
