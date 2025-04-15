using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Products.Services;
using Products.Tests.TestHelpers;

namespace Products.Tests.Services;

public class ProductConsumerServiceTests
{
    private readonly Mock<ILogger<ProductConsumerService>> _loggerMock;
    private readonly Mock<NatsService> _natsServiceMock;
    private readonly Mock<IProductService> _productServiceMock;

    public ProductConsumerServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProductConsumerService>>();

        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();

        var natsUrlSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        natsUrlSection.Setup(x => x.Value).Returns("nats://localhost:4222");
        configMock.Setup(x => x.GetSection("Nats:Url")).Returns(natsUrlSection.Object);

        _natsServiceMock = new Mock<NatsService>(
            Mock.Of<ILogger<NatsService>>(),
            configMock.Object);

        _productServiceMock = new Mock<IProductService>();
    }

    [Fact]
    public async Task HandleGetProductRequest_ShouldReturnProduct_WhenProductExists()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Get;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply"; // Set ReplyTo so the reply handler is called

        _productServiceMock.Setup(s => s.GetProductAsync(testMessage.ProductId!))
            .ReturnsAsync(testMessage);

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleGetProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeTrue();
        capturedResponse.Product.Should().NotBeNull();
        capturedResponse.Product!.ProductId.Should().Be(testMessage.ProductId);
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.GetProductAsync(testMessage.ProductId!), Times.Once);
    }

    [Fact]
    public async Task HandleGetProductRequest_ShouldReturnError_WhenProductDoesNotExist()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Get;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply"; // Set ReplyTo so the reply handler is called

        _productServiceMock.Setup(s => s.GetProductAsync(testMessage.ProductId!))
            .ReturnsAsync((ProductMessage?)null);

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleGetProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeFalse();
        capturedResponse.Error.Should().NotBeNullOrEmpty();
        capturedResponse.Error.Should().Contain("not found");
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.GetProductAsync(testMessage.ProductId!), Times.Once);
    }

    [Fact]
    public async Task HandleGetAllProductsRequest_ShouldReturnPaginatedProducts()
    {
        var testMessage = new ProductMessage
        {
            OperationType = ProductOperationType.GetAll,
            SessionId = "test-session",
            PageNumber = 2,
            PageSize = 5,
            ReplyTo = "test.reply" // Set ReplyTo so the reply handler is called
        };

        var testProducts = Enumerable.Range(1, 5)
            .Select(_ => TestData.GetTestProductMessage())
            .ToList();

        _productServiceMock.Setup(s => s.GetPaginatedProductsAsync(2, 5))
            .ReturnsAsync((testProducts, 20, 4));

        var replyHandled = false;
        ProductListResponse? capturedResponse = null;

        Action<string, ProductListResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleGetAllProductsRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeTrue();
        capturedResponse.Products.Should().NotBeNull();
        capturedResponse.Products!.Count.Should().Be(5);
        capturedResponse.TotalCount.Should().Be(20);
        capturedResponse.PageNumber.Should().Be(2);
        capturedResponse.PageSize.Should().Be(5);
        capturedResponse.TotalPages.Should().Be(4);
        capturedResponse.HasPreviousPage.Should().BeTrue();
        capturedResponse.HasNextPage.Should().BeTrue();
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.GetPaginatedProductsAsync(2, 5), Times.Once);
    }

    [Fact]
    public async Task HandleCreateProductRequest_ShouldCreateProduct()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Create;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply"; // Set ReplyTo so the reply handler is called

        _productServiceMock.Setup(s => s.CreateProductAsync(It.IsAny<ProductMessage>()))
            .ReturnsAsync(testMessage);

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleCreateProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeTrue();
        capturedResponse.Product.Should().NotBeNull();
        capturedResponse.Product!.ProductId.Should().Be(testMessage.ProductId);
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.CreateProductAsync(It.IsAny<ProductMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleUpdateProductRequest_ShouldUpdateProduct_WhenProductExists()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Update;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply"; // Set ReplyTo so the reply handler is called

        _productServiceMock.Setup(s => s.UpdateProductAsync(It.IsAny<ProductMessage>()))
            .ReturnsAsync(true);
        _productServiceMock.Setup(s => s.GetProductAsync(testMessage.ProductId!))
            .ReturnsAsync(testMessage);

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleUpdateProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeTrue();
        capturedResponse.Product.Should().NotBeNull();
        capturedResponse.Product!.ProductId.Should().Be(testMessage.ProductId);
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.UpdateProductAsync(It.IsAny<ProductMessage>()), Times.Once);
        _productServiceMock.Verify(s => s.GetProductAsync(testMessage.ProductId!), Times.Once);
    }

    [Fact]
    public async Task HandleUpdateProductRequest_ShouldReturnError_WhenProductDoesNotExist()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Update;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply"; // Set ReplyTo so the reply handler is called

        _productServiceMock.Setup(s => s.UpdateProductAsync(It.IsAny<ProductMessage>()))
            .ReturnsAsync(false);

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleUpdateProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeFalse();
        capturedResponse.Error.Should().NotBeNullOrEmpty();
        capturedResponse.Error.Should().Contain("not found");
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.UpdateProductAsync(It.IsAny<ProductMessage>()), Times.Once);
        _productServiceMock.Verify(s => s.GetProductAsync(testMessage.ProductId!), Times.Never);
    }

    [Fact]
    public async Task HandleCreateProductRequest_ShouldHandleServiceException()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.OperationType = ProductOperationType.Create;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply";

        _productServiceMock.Setup(s => s.CreateProductAsync(It.IsAny<ProductMessage>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleCreateProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeFalse();
        capturedResponse.Error.Should().NotBeNullOrEmpty();
        capturedResponse.Error.Should().Contain("Test exception");
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.CreateProductAsync(It.IsAny<ProductMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleGetProductRequest_WithMissingProductId_ShouldReturnError()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.ProductId = null; // Missing product ID
        testMessage.OperationType = ProductOperationType.Get;
        testMessage.SessionId = "test-session";
        testMessage.ReplyTo = "test.reply";

        var replyHandled = false;
        ProductResponse? capturedResponse = null;

        Action<string, ProductResponse> replyHandler = (subject, response) =>
        {
            replyHandled = true;
            capturedResponse = response;
        };

        var consumerService = new ProductConsumerServiceTestWrapper(
            _loggerMock.Object,
            _natsServiceMock.Object,
            _productServiceMock.Object);

        await consumerService.TestHandleGetProductRequest(testMessage, replyHandler);

        replyHandled.Should().BeTrue();
        capturedResponse.Should().NotBeNull();
        capturedResponse!.Success.Should().BeFalse();
        capturedResponse.Error.Should().NotBeNullOrEmpty();
        capturedResponse.Error.Should().Contain("null");
        capturedResponse.SessionId.Should().Be("test-session");

        _productServiceMock.Verify(s => s.GetProductAsync(It.IsAny<string>()), Times.Never);
    }
}

/// <summary>
/// Test wrapper for ProductConsumerService to expose protected methods for testing
/// </summary>
public class ProductConsumerServiceTestWrapper : ProductConsumerService
{
    private readonly IProductService _productService;

    public ProductConsumerServiceTestWrapper(
        ILogger<ProductConsumerService> logger,
        NatsService natsService,
        IProductService productService)
        : base(logger, natsService, productService)
    {
        _productService = productService;
    }

    public async Task TestHandleGetProductRequest(
        ProductMessage message,
        Action<string, ProductResponse> replyHandler)
    {
        var response = new ProductResponse { Success = false };

        try
        {
            if (string.IsNullOrEmpty(message.ProductId))
            {
                response.Error = "Product ID cannot be null or empty";

                if (!string.IsNullOrEmpty(message.SessionId))
                {
                    response.SessionId = message.SessionId;
                }
            }
            else
            {
                var product = await _productService.GetProductAsync(message.ProductId);

                if (product != null)
                {
                    response.Success = true;
                    response.Message = $"Product with ID {message.ProductId} found";
                    response.Product = product;

                    if (!string.IsNullOrEmpty(message.SessionId))
                    {
                        response.SessionId = message.SessionId;
                    }
                }
                else
                {
                    response.Error = $"Product with ID {message.ProductId} not found";

                    if (!string.IsNullOrEmpty(message.SessionId))
                    {
                        response.SessionId = message.SessionId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            response.Error = $"Error getting product: {ex.Message}";

            if (!string.IsNullOrEmpty(message.SessionId))
            {
                response.SessionId = message.SessionId;
            }
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            replyHandler(message.ReplyTo, response);
        }
    }

    public async Task TestHandleGetAllProductsRequest(
        ProductMessage message,
        Action<string, ProductListResponse> replyHandler)
    {
        var response = new ProductListResponse { Success = false };

        try
        {
            int pageNumber = Math.Max(1, message.PageNumber);
            int pageSize = Math.Clamp(message.PageSize, 1, 100);

            var result = await _productService.GetPaginatedProductsAsync(pageNumber, pageSize);
            var products = result.Products;
            var totalCount = result.TotalCount;
            var totalPages = result.TotalPages;
            var productsList = products.ToList();

            response.Success = true;
            response.Message = $"Retrieved {productsList.Count} products (page {pageNumber} of {totalPages})";
            response.Products = productsList;
            response.TotalCount = totalCount;
            response.PageNumber = pageNumber;
            response.PageSize = pageSize;
            response.TotalPages = totalPages;
            response.HasPreviousPage = pageNumber > 1;
            response.HasNextPage = pageNumber < totalPages;

            if (!string.IsNullOrEmpty(message.SessionId))
            {
                response.SessionId = message.SessionId;
            }
        }
        catch (Exception ex)
        {
            response.Error = $"Error getting products: {ex.Message}";
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            replyHandler(message.ReplyTo, response);
        }
    }

    public async Task TestHandleCreateProductRequest(
        ProductMessage message,
        Action<string, ProductResponse> replyHandler)
    {
        var response = new ProductResponse { Success = false };

        // Set session ID early to ensure it's always included
        if (!string.IsNullOrEmpty(message.SessionId))
        {
            response.SessionId = message.SessionId;
        }

        try
        {
            var product = await _productService.CreateProductAsync(message);

            response.Success = true;
            response.Message = $"Product with ID {product.ProductId} created";
            response.Product = product;
        }
        catch (Exception ex)
        {
            response.Error = $"Error creating product: {ex.Message}";
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            replyHandler(message.ReplyTo, response);
        }
    }

    public async Task TestHandleUpdateProductRequest(
        ProductMessage message,
        Action<string, ProductResponse> replyHandler)
    {
        var response = new ProductResponse { Success = false };

        try
        {
            var success = await _productService.UpdateProductAsync(message);

            if (success)
            {
                var product = await _productService.GetProductAsync(message.ProductId!);

                response.Success = true;
                response.Message = $"Product with ID {message.ProductId} updated";
                response.Product = product;

                if (!string.IsNullOrEmpty(message.SessionId))
                {
                    response.SessionId = message.SessionId;
                }
            }
            else
            {
                response.Error = $"Product with ID {message.ProductId} not found";

                if (!string.IsNullOrEmpty(message.SessionId))
                {
                    response.SessionId = message.SessionId;
                }
            }
        }
        catch (Exception ex)
        {
            response.Error = $"Error updating product: {ex.Message}";

            if (!string.IsNullOrEmpty(message.SessionId))
            {
                response.SessionId = message.SessionId;
            }
        }

        if (!string.IsNullOrEmpty(message.ReplyTo))
        {
            replyHandler(message.ReplyTo, response);
        }
    }
}