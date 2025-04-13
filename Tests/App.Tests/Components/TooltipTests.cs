using Bunit;
using FluentAssertions;
using Frontend.Components;

namespace App.Tests.Components;

public class TooltipTests : TestContext
{
    [Fact]
    public void Tooltip_ShouldRenderContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Tooltip>(parameters =>
        {
            parameters.Add(p => p.Content, "Test Content");
            parameters.Add(p => p.TooltipText, "Tooltip Text");
        });

        // Assert
        cut.Markup.Should().Contain("Test Content");
        cut.Markup.Should().NotContain("Tooltip Text"); // Tooltip is hidden by default
    }

    [Fact]
    public void Tooltip_ShouldShowTooltipOnMouseOver()
    {
        // Arrange
        var cut = RenderComponent<Tooltip>(parameters =>
        {
            parameters.Add(p => p.Content, "Test Content");
            parameters.Add(p => p.TooltipText, "Tooltip Text");
        });

        // Act - simulate mouse over
        cut.Find(".tooltip-content").MouseOver();

        // Assert
        cut.Markup.Should().Contain("Tooltip Text");
        cut.Markup.Should().Contain("custom-tooltip");
    }

    [Fact]
    public void Tooltip_ShouldHideTooltipOnMouseOut()
    {
        // Arrange
        var cut = RenderComponent<Tooltip>(parameters =>
        {
            parameters.Add(p => p.Content, "Test Content");
            parameters.Add(p => p.TooltipText, "Tooltip Text");
        });

        // Act - simulate mouse over then mouse out
        cut.Find(".tooltip-content").MouseOver();
        cut.Find(".tooltip-content").MouseOut();

        // Assert
        cut.Markup.Should().NotContain("Tooltip Text");
    }

    [Fact]
    public void Tooltip_ShouldApplyCorrectPositionClass()
    {
        // Arrange & Act
        var cut = RenderComponent<Tooltip>(parameters =>
        {
            parameters.Add(p => p.Content, "Test Content");
            parameters.Add(p => p.TooltipText, "Tooltip Text");
            parameters.Add(p => p.Position, "bottom");
        });

        // Act - simulate mouse over
        cut.Find(".tooltip-content").MouseOver();

        // Assert
        cut.Markup.Should().Contain("tooltip-bottom");
    }

    [Fact]
    public void Tooltip_ShouldApplyCustomWidth()
    {
        // Arrange & Act
        var cut = RenderComponent<Tooltip>(parameters =>
        {
            parameters.Add(p => p.Content, "Test Content");
            parameters.Add(p => p.TooltipText, "Tooltip Text");
            parameters.Add(p => p.Width, "200px");
        });

        // Act - simulate mouse over
        cut.Find(".tooltip-content").MouseOver();

        // Assert
        cut.Markup.Should().Contain("width: 200px");
    }
}
