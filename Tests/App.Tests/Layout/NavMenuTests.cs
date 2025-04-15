using Bunit;
using FluentAssertions;
using Frontend.Layout;
using Frontend.Models;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using App.Tests.TestHelpers;

namespace App.Tests.Layout;

public class NavMenuTests : TestContext
{
    private readonly Mock<ICartService> _mockCartService;

    public NavMenuTests()
    {
        this.AddMudBlazorTestServices();

        _mockCartService = new Mock<ICartService>();
        _mockCartService.Setup(s => s.GetCartAsync()).ReturnsAsync(new ShoppingCart
        {
            Items = new List<CartItem>(),
            TotalPrice = 0,
            ItemCount = 0
        });
        Services.AddSingleton(_mockCartService.Object);
    }

    [Fact]
    public void NavMenu_ShouldRender_WithLinks_WhenDrawerOpen()
    {
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.DrawerOpen, true));

        cut.Markup.Should().Contain("href=\"\""); // Home link has empty href
        cut.Markup.Should().Contain("href=\"products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"cart\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"admin/products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"about\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("Home");
        cut.Markup.Should().Contain("Products");
        cut.Markup.Should().Contain("Cart");
        cut.Markup.Should().Contain("Admin");
        cut.Markup.Should().Contain("About");
    }

    [Fact]
    public void NavMenu_ShouldRender_WithoutText_WhenDrawerClosed()
    {
        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.DrawerOpen, false));

        cut.Markup.Should().Contain("href=\"\""); // Home link has empty href
        cut.Markup.Should().Contain("href=\"products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"cart\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"admin/products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"about\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().NotContain("<span>Home</span>");
        cut.Markup.Should().NotContain("<span>Products</span>");
        cut.Markup.Should().NotContain("<span>Cart</span>");
        cut.Markup.Should().NotContain("<span>Admin</span>");
        cut.Markup.Should().NotContain("<span>About</span>");
    }

    [Fact]
    public void NavMenu_ShouldShowCartCount_WhenCartHasItems_AndDrawerOpen()
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

        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.DrawerOpen, true));

        cut.Markup.Should().Contain("3");
        cut.Markup.Should().Contain("Cart");
    }

    [Fact]
    public void NavMenu_ShouldShowCartCount_WhenCartHasItems_AndDrawerClosed()
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

        var cut = RenderComponent<NavMenu>(parameters => parameters
            .Add(p => p.DrawerOpen, false));

        cut.Markup.Should().Contain("3");
        cut.Markup.Should().NotContain("<span>Cart</span>");
    }
}
