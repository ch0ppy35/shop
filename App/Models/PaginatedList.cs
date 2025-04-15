namespace Frontend.Models;

/// <summary>
/// Represents a paginated list of items
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PaginatedList<T>
{
    /// <summary>
    /// Gets or sets the items in the current page
    /// </summary>
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Gets or sets the total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}