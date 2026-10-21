namespace eFinanzer.Common.Pagination.Errors;

public record InvalidOffsetPaginationError(OffsetPagination Pagination, int MaxPageSize) 
    : Error(Pagination switch
    {
        { Offset: < 0} => "Offset must not be negative",
        { PageSize: <= 0 } => "PageSize must be greater than zero",
        _ => $"PageSize {Pagination.PageSize} exceeds the maximum of {MaxPageSize}"
    });