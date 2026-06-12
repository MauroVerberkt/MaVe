namespace MaVe.Railyard;

/// <summary>
/// Metadata describing a registered operation.
/// </summary>
public sealed record OperationDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationDescriptor" /> class.
    /// </summary>
    /// <param name="name">The operation dispatch name.</param>
    /// <param name="description">Optional human-readable description.</param>
    public OperationDescriptor(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
    }

    /// <summary>
    /// The dispatch name of the operation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional human-readable description.
    /// </summary>
    public string? Description { get; }
}
