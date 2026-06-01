namespace HelperUnions;

/// <summary>
/// Marks a partial record as a discriminated union declaration target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UnionAttribute : Attribute
{
}
