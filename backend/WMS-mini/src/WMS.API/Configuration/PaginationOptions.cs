using System.ComponentModel.DataAnnotations;

namespace WMS.API.Configuration;

public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";

    [Range(1, int.MaxValue)]
    public int PageSize { get; init; } = 10;
}
