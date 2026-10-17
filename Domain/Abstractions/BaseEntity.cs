namespace MyBon.Domain.Abstractions;

/// <summary>
/// Base class for domain entities. Holds the audit fields shared by every table.
/// Primary keys and navigation properties stay on the concrete entity.
/// </summary>
public abstract class BaseEntity : IEntity
{
    /// <summary>Gets or sets the UTC timestamp of when the row was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // TODO: add UpdatedAt / IsDeleted here once the matching columns exist in the database.
}
