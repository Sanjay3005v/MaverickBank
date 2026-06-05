namespace MaverickBank.DTOs.Pagination
{
    public record PagedResultDto<T>(
        IEnumerable<T> Data,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages
    );
}
