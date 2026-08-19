namespace WMS.Application.DTOs;

public class ProductListQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
}
