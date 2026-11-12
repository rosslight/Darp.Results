using System.Collections.ObjectModel;

namespace Darp.Results;

partial struct Result<TValue, TError>
{
    /// <summary> Creates a new result with the additional metadata key-value pair. </summary>
    /// <param name="key"> The key of the metadata. </param>
    /// <param name="value"> The value of the metadata. </param>
    /// <returns> The result with the new metadata. </returns>
    public Result<TValue, TError> WithMetadata(string key, object value) =>
        ReplaceMetadata(new Dictionary<string, object>(Metadata) { [key] = value });

    /// <summary> Creates a new result with the additional metadata keys </summary>
    /// <param name="metadata"> The metadata of the result.</param>
    /// <returns> The result with the new metadata. </returns>
    public Result<TValue, TError> WithMetadata(ICollection<KeyValuePair<string, object>> metadata) =>
        ReplaceMetadata(new ReadOnlyDictionary<string, object>(Metadata.Concat(metadata).ToDictionary()));

    private Result<TValue, TError> ReplaceMetadata(IReadOnlyDictionary<string, object> metadata) =>
        new(_value, _error, metadata, _state);
}
