namespace MyBon.Models.Common;

/// <summary>
/// One page of a longer list together with the total row count, so the list title can report
/// every match while the page only paints the rows it was asked for.
/// </summary>
public class PagedResult<T>
{
    /// <summary>Rows of the requested page, in list order.</summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Rows across every page, not only the rows carried by this one.</summary>
    public int Total { get; set; }

    /// <summary>One-based number of this page.</summary>
    public int Page { get; set; }

    /// <summary>Rows per page, fixed by the service that built the result.</summary>
    public int PageSize { get; set; }

    /// <summary>Whether another page follows this one.</summary>
    public bool HasMore => Page * PageSize < Total;
}
