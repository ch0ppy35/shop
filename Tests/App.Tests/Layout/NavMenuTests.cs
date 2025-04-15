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

        var mockJsRuntime = new Mock<IJSRuntime>();
        mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync((object)null!);
        Services.AddSingleton(mockJsRuntime.Object);
    }

    [Fact]
    public void NavMenu_ShouldRender_WithLinks()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("href=\"\""); // Home link has empty href
        cut.Markup.Should().Contain("href=\"products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"cart\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"admin/products\""); // MudBlazor doesn't add leading slash
        cut.Markup.Should().Contain("href=\"about\""); // MudBlazor doesn't add leading slash
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

        cut.Markup.Should().Contain("3");
    }
}
