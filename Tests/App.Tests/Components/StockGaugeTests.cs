using Bunit;
using FluentAssertions;
using Frontend.Components;

namespace App.Tests.Components;

public class StockGaugeTests : TestContext
{
    private IRenderedComponent<StockGauge> Render(int quantity, int reorderThreshold) =>
        RenderComponent<StockGauge>(p => p
            .Add(g => g.Quantity, quantity)
            .Add(g => g.ReorderThreshold, reorderThreshold));

    [Fact]
    public void ShouldShowStockCount_WhenAboveReorderPoint()
    {
        var cut = Render(40, 10);

        cut.Markup.Should().Contain("40 in stock");
        cut.Find(".gauge").ClassList.Should().NotContain("is-low");
        cut.FindAll(".gauge-cells .on").Should().HaveCount(10);
    }

    [Fact]
    public void ShouldWarn_WhenAtOrBelowReorderPoint()
    {
        var cut = Render(8, 10);

        cut.Markup.Should().Contain("Only 8 left");
        cut.Find(".gauge").ClassList.Should().Contain("is-low");
        cut.FindAll(".gauge-cells .on").Should().HaveCount(2);
    }

    [Fact]
    public void ShouldShowOutOfStock_WhenEmpty()
    {
        var cut = Render(0, 10);

        cut.Markup.Should().Contain("Out of stock");
        cut.FindAll(".gauge-cells .on").Should().BeEmpty();
    }
}
