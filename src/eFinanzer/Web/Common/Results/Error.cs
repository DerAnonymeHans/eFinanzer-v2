namespace eFinanzer.Common.Results;

public abstract record Error(string Message, Error? ParentError = null);