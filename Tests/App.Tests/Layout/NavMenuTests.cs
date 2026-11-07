using App.Tests.TestHelpers;
using Bunit;
using FluentAssertions;
using Frontend.Layout;
using Frontend.Models;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace App.Tests.Layout;

public class NavMenuTests : TestContext
{
    private readonly Mock<ICartService> _mockCartService;

    public NavMenuTests()
    {
        _mockCartService = this.AddAppTestServices();
    }

    [Fact]
    public void NavMenu_ShouldRender_AllLinks()
    {
        var cut = RenderComponent<NavMenu>();

        var hrefs = cut.FindAll("a").Select(a => a.GetAttribute("href"));
        hrefs.Should().Equal("products", "cart", "admin/products", "about");
        cut.Markup.Should().Contain("Products").And.Contain("Cart").And.Contain("Admin").And.Contain("About");
    }

    [Fact]
    public void NavMenu_ShouldHideCartCount_WhenCartIsEmpty()
    {
        var cut = RenderComponent<NavMenu>();

        cut.FindAll(".cart-count").Should().BeEmpty();
    }

    [Fact]
    public void NavMenu_ShouldShowCartCount_WhenCartHasItems()
    {
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = "1", Quantity = 2 },
                new() { ProductId = "2", Quantity = 1 }
            },
            TotalPrice = 29.99m,
            ItemCount = 3
        });

        var cut = RenderComponent<NavMenu>();

        cut.WaitForAssertion(() => cut.Find(".cart-count").TextContent.Should().Be("3"));
    }

    [Fact]
    public void NavMenu_ShouldUpdateCartCount_WhenCartStateChanges()
    {
        var cut = RenderComponent<NavMenu>();

        Services.GetRequiredService<CartState>().SetItemCount(5);

        cut.WaitForAssertion(() => cut.Find(".cart-count").TextContent.Should().Be("5"));
    }
}
