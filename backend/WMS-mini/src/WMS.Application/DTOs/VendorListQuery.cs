namespace WMS.Application.DTOs;

public class VendorListQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
}