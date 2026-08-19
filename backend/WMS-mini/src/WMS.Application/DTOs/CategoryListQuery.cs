namespace WMS.Application.DTOs;

public class CategoryListQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
}
