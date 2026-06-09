using System.Text.Json;

namespace MaVe.BusinessRulesAnalyzer;

/// <summary>
/// Extension methods for <see cref="JsonElement" /> to provide case-insensitive property lookups.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Attempts to get a property from a <see cref="JsonElement" /> using a case-insensitive comparison.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The property name to match (case-insensitive).</param>
    /// <param name="value">When this method returns, contains the property value if found; otherwise, the default value.</param>
    /// <returns><c>true</c> if a matching property was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
