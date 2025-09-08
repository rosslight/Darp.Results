using System.Collections.ObjectModel;
using System.Diagnostics;
using static Darp.Results.Result;

namespace Darp.Results;

partial class Result<TValue, TError>
{
    /// <summary> Creates a new result with the additional metadata key-value pair. </summary>
    /// <param name="key"> The key of the metadata. </param>
    /// <param name="value"> The value of the metadata. </param>
    /// <returns> The result with the new metadata. </returns>
    public Result<TValue, TError> WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
        return this switch
        {
            Ok<TValue, TError>(var v) => new Ok<TValue, TError>(v, readOnlyMetadata: newMetadata),
            Err<TValue, TError>(var error) => new Err<TValue, TError>(error, readOnlyMetadata: newMetadata),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary> Creates a new result with the additional metadata keys </summary>
    /// <param name="metadata"> The metadata of the result.</param>
    /// <returns> The result with the new metadata. </returns>
    public Result<TValue, TError> WithMetadata(ICollection<KeyValuePair<string, object>> metadata)
    {
        var newMetadata = new ReadOnlyDictionary<string, object>(Metadata.Concat(metadata).ToDictionary());
        return this switch
        {
            Ok<TValue, TError>(var v) => new Ok<TValue, TError>(v, readOnlyMetadata: newMetadata),
            Err<TValue, TError>(var error) => new Err<TValue, TError>(error, readOnlyMetadata: newMetadata),
            _ => throw new UnreachableException(),
        };
    }
}
