using Common.Models;
using Microsoft.Extensions.Logging;
using Products.Repositories;

namespace Products.Services;

/// <summary>
/// Service for managing products
/// </summary>
public class ProductService : IProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    public ProductService(ILogger<ProductService> logger, IProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;

        _logger.LogInformation("ProductService initialized");
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    public async Task<IEnumerable<ProductMessage>> GetAllProductsAsync()
    {
        _logger.LogInformation("Getting all products from database");
        var products = await _productRepository.GetAllProductsAsync();
        return products.Select(p => ProductRepository.ToProductMessage(p));
    }

    /// <summary>
    /// Gets paginated products
    /// </summary>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A tuple containing the paginated products and pagination metadata</returns>
    public async Task<(IEnumerable<ProductMessage> Products, int TotalCount, int TotalPages)> GetPaginatedProductsAsync(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Getting paginated products: Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);

        // Get paginated products from repository
        var (products, totalCount) = await _productRepository.GetPaginatedProductsAsync(pageNumber, pageSize);

        // Convert to ProductMessage objects
        var productMessages = products.Select(p => ProductRepository.ToProductMessage(p)).ToList();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return (productMessages, totalCount, totalPages);
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    public async Task<ProductMessage?> GetProductAsync(string id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);

        var product = await _productRepository.GetProductByIdAsync(id);
        if (product != null)
        {
            return ProductRepository.ToProductMessage(product);
        }

        _logger.LogWarning("Product with ID {ProductId} not found", id);
        return null;
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    public async Task<ProductMessage> CreateProductAsync(ProductMessage product)
    {
        _logger.LogInformation("Creating new product with ID: {ProductId}", product.ProductId);

        if (string.IsNullOrEmpty(product.ProductId))
        {
            product.ProductId = Guid.NewGuid().ToString();
        }

        var entity = ProductRepository.ToProductEntity(product);
        var createdProduct = await _productRepository.CreateProductAsync(entity);

        return ProductRepository.ToProductMessage(createdProduct, ProductOperationType.Create);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    public async Task<bool> UpdateProductAsync(ProductMessage product)
    {
        if (string.IsNullOrEmpty(product.ProductId))
        {
            _logger.LogWarning("Cannot update product with null or empty ID");
            return false;
        }

        _logger.LogInformation("Updating product with ID: {ProductId}", product.ProductId);

        var entity = ProductRepository.ToProductEntity(product);
        return await _productRepository.UpdateProductAsync(entity);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    public async Task<bool> DeleteProductAsync(string id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        return await _productRepository.DeleteProductAsync(id);
    }

    /// <summary>
    /// Updates inventory for a product
    /// </summary>
    public async Task<bool> UpdateInventoryAsync(ProductMessage product)
    {
        if (string.IsNullOrEmpty(product.ProductId))
        {
            _logger.LogWarning("Cannot update inventory with null or empty product ID");
            return false;
        }

        _logger.LogInformation("Updating inventory for product with ID: {ProductId}", product.ProductId);

        // First check if the product exists
        var existingProduct = await _productRepository.GetProductByIdAsync(product.ProductId);
        if (existingProduct == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for inventory update", product.ProductId);
            return false;
        }

        // Update only the inventory fields
        existingProduct.Sku = product.Sku ?? existingProduct.Sku;
        existingProduct.Location = product.Location ?? existingProduct.Location;
        existingProduct.QuantityInStock = product.QuantityInStock;
        existingProduct.ReorderThreshold = product.ReorderThreshold;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        return await _productRepository.UpdateProductAsync(existingProduct);
    }

    /// <summary>
    /// Gets inventory for a product
    /// </summary>
    public async Task<ProductMessage?> GetInventoryAsync(string id)
    {
        _logger.LogInformation("Getting inventory for product with ID: {ProductId}", id);

        var product = await _productRepository.GetProductByIdAsync(id);
        if (product != null)
        {
            var message = ProductRepository.ToProductMessage(product, ProductOperationType.GetInventory);
            return message;
        }

        _logger.LogWarning("Product with ID {ProductId} not found for inventory query", id);
        return null;
    }

    /// <summary>
    /// Updates inventory for a product
    /// </summary>
    public async Task<bool> UpdateInventoryAsync(string id, int quantity)
    {
        _logger.LogInformation("Updating inventory for product with ID: {ProductId} to quantity: {Quantity}", id, quantity);

        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("Cannot update inventory with null or empty product ID");
            return false;
        }

        var product = await _productRepository.GetProductByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for inventory update", id);
            return false;
        }

        // Update the inventory quantity
        product.QuantityInStock = quantity;
        product.UpdatedAt = DateTime.UtcNow;

        // Save the changes
        return await _productRepository.UpdateProductAsync(product);
    }
}
