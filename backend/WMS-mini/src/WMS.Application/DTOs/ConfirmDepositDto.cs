using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class ConfirmDepositDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền cọc thực nhận phải lớn hơn 0.")]
    public decimal PaidDeposit { get; set; }

    [MaxLength(500)]
    public string? DepositNotes { get; set; }
}
