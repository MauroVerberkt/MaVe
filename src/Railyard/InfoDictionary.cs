using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace MaVe.Railyard;

/// <summary>
/// Represents a read-only dictionary with string keys (names) and values (infos).
/// </summary>
public class InfoDictionary : ReadOnlyDictionary<string, string>
{
    /// <summary>
    /// The list of infos (values) from the dictionary.
    /// </summary>
    private readonly List<string> _infoBackingField;

    /// <summary>
    /// The list of names (keys) from the dictionary.
    /// </summary>
    private readonly List<string> _nameBackingField;

    /// <summary>
    /// Initializes a new instance of the <see cref="InfoDictionary" /> class.
    /// </summary>
    /// <param name="dictionary">The dictionary to initialize the <see cref="InfoDictionary" /> with.</param>
    public InfoDictionary(IDictionary<string, string> dictionary) : base(dictionary)
    {
        _nameBackingField = base.Keys.ToList();
        _infoBackingField = base.Values.ToList();
    }

    /// <summary>
    /// Gets the list of keys. This property is not supported and throws a
    /// <see cref="NotSupportedException" />. Use <see cref="Name" /> instead.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown when accessed.</exception>
    public new List<string> Keys
    {
        [DoesNotReturn]
        get => throw new NotSupportedException(
            $"{nameof(Keys)} is not supported for {nameof(InfoDictionary)}, please use {nameof(Name)} instead.");
    }

    /// <summary>
    /// Gets the list of values. This property is not supported and throws a
    /// <see cref="NotSupportedException" />. Use <see cref="Info" /> instead.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown when accessed.</exception>
    public new List<string> Values
    {
        [DoesNotReturn]
        get => throw new NotSupportedException(
            $"{nameof(Values)} is not supported for {nameof(InfoDictionary)}, please use {nameof(Info)} instead.");
    }

    /// <summary>
    /// Gets a read-only list of names (keys).
    /// </summary>
    public IReadOnlyList<string> Name => _nameBackingField;

    /// <summary>
    /// Gets a read-only list of infos (values).
    /// </summary>
    public IReadOnlyList<string> Info => _infoBackingField;

    /// <summary>
    /// Converts the dictionary to a JSON string representation.
    /// </summary>
    /// <returns>A JSON string representing the dictionary.</returns>
    public string ToJsonString()
    {
        var dictionary = Name.Zip(Info, (name, info) => new { name, info })
            .ToDictionary(x => x.name, x => x.info);

        return JsonSerializer.Serialize(dictionary);
    }
}
