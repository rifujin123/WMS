namespace WMS.Application.DTOs;

public class UserListQuery
{
    public int Page { get; init; } = 1;
    public string? Role { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
}
