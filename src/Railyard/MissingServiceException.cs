namespace MaVe.Railyard;

/// <summary>
/// Exception thrown when required services are missing from the service collection.
/// </summary>
/// <param name="missingServices">
/// A collection of missing service names.
/// </param>
/// <param name="delimiter">
/// A string used to separate the names of the missing services in the exception message. Defaults to ", ".
/// </param>
public class MissingServiceException(
    IEnumerable<string> missingServices,
    string delimiter = MissingServiceException.DefaultDelimiter)
    : Exception($"The following required services are missing: {string.Join(delimiter, missingServices)}")
{
    /// <summary>
    /// The default delimiter used to separate service names in the exception message.
    /// </summary>
    private const string DefaultDelimiter = ", ";
}
