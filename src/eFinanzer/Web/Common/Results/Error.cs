namespace eFinanzer.Common.Results;

public abstract record Error(string Message, Error? ParentError = null)
{
    /// <summary>
    /// Class name of the error, so clients can tell errors with the same status code apart. Computed, so it is
    /// serialized but not part of the record equality.
    /// </summary>
    public string Type => GetType().Name;
}
