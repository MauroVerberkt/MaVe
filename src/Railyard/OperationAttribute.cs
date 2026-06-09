namespace MaVe.Railyard;

/// <summary>
/// Marks a class as a Railyard operation and assigns its dispatch name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OperationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAttribute" /> class.
    /// </summary>
    /// <param name="name">Unique operation name used for dispatch routing.</param>
    public OperationAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the dispatch name for this operation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets an optional human-readable description of the operation.
    /// </summary>
    public string? Description { get; init; }
}
