using Bunit;
using Frontend.Layout;
using Frontend.Models;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace App.Tests.Layout;

public class NavMenuTests : TestContext
{
    public NavMenuTests()
    {
        // Register mock CartService
        var mockCartService = new Mock<ICartService>();
        mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart
        {
            Items = new List<CartItem>(),
            TotalPrice = 0,
            ItemCount = 0
        });
        Services.AddSingleton(mockCartService.Object);

        // Register mock IJSRuntime
        var mockJsRuntime = new Mock<Microsoft.JSInterop.IJSRuntime>();
        mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);
        Services.AddSingleton(mockJsRuntime.Object);

        // Register mock ToastService
        var mockToastService = new Mock<ToastService>();
        Services.AddSingleton(mockToastService.Object);

        // Register mock ConfirmService
        var mockConfirmService = new Mock<IConfirmService>();
        Services.AddSingleton(mockConfirmService.Object);
    }

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
        Assert.Equal(5, navLinks.Count);

        // Check the Home link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Home"));

        // Check the About link
        Assert.Contains(navLinks, link => link.TextContent.Contains("About"));

        // Check the Admin link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Admin"));

        // Check the Products link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Products"));

        // Check the Cart link
        Assert.Contains(navLinks, link => link.TextContent.Contains("Cart"));

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
