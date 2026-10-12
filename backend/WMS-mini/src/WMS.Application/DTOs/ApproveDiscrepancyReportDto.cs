namespace WMS.Application.DTOs;

public class ApproveDiscrepancyReportDto
{
    public bool IsApproved { get; set; } = true;
    public string? Notes { get; set; }
}
