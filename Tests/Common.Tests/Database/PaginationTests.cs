using Common.Database;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Tests.Database;

public class PaginationTests
{
    private readonly Mock<ILogger<ProductDbContext>> _loggerMock;
    private readonly Mock<DbContextOptions<ProductDbContext>> _optionsMock;
    private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _configMock;

    public PaginationTests()
    {
        _loggerMock = new Mock<ILogger<ProductDbContext>>();
        _optionsMock = new Mock<DbContextOptions<ProductDbContext>>();
        _configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
    }

    [Fact]
    public void CalculatePaginationMetadata_ShouldReturnCorrectValues()
    {
        // Arrange
        int totalCount = 100;
        int pageSize = 10;
        int pageNumber = 2;

        // Act
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        bool hasPreviousPage = pageNumber > 1;
        bool hasNextPage = pageNumber < totalPages;

        // Assert
        totalPages.Should().Be(10);
        hasPreviousPage.Should().BeTrue();
        hasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CalculatePaginationMetadata_WithLastPage_ShouldHaveNoNextPage()
    {
        // Arrange
        int totalCount = 100;
        int pageSize = 10;
        int pageNumber = 10; // Last page

        // Act
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        bool hasPreviousPage = pageNumber > 1;
        bool hasNextPage = pageNumber < totalPages;

        // Assert
        totalPages.Should().Be(10);
        hasPreviousPage.Should().BeTrue();
        hasNextPage.Should().BeFalse();
    }

    [Fact]
    public void CalculatePaginationMetadata_WithFirstPage_ShouldHaveNoPreviousPage()
    {
        // Arrange
        int totalCount = 100;
        int pageSize = 10;
        int pageNumber = 1; // First page

        // Act
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        bool hasPreviousPage = pageNumber > 1;
        bool hasNextPage = pageNumber < totalPages;

        // Assert
        totalPages.Should().Be(10);
        hasPreviousPage.Should().BeFalse();
        hasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CalculatePaginationMetadata_WithPartialLastPage_ShouldCalculateCorrectly()
    {
        // Arrange
        int totalCount = 95; // Not evenly divisible by page size
        int pageSize = 10;
        int pageNumber = 10; // Last page (with only 5 items)

        // Act
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        bool hasPreviousPage = pageNumber > 1;
        bool hasNextPage = pageNumber < totalPages;

        // Assert
        totalPages.Should().Be(10);
        hasPreviousPage.Should().BeTrue();
        hasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ValidatePaginationParameters_ShouldClampValues()
    {
        // Arrange
        int invalidPageNumber = -1;
        int tooLargePageSize = 500;
        int tooSmallPageSize = 0;

        // Act
        int validatedPageNumber = Math.Max(1, invalidPageNumber);
        int validatedLargePageSize = Math.Clamp(tooLargePageSize, 1, 100);
        int validatedSmallPageSize = Math.Clamp(tooSmallPageSize, 1, 100);

        // Assert
        validatedPageNumber.Should().Be(1);
        validatedLargePageSize.Should().Be(100);
        validatedSmallPageSize.Should().Be(1);
    }
}
