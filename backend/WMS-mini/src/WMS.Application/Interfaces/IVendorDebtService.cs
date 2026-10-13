using WMS.Application.DTOs;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IVendorDebtService
{
    Task<List<VendorDebtLedgerDto>> GetAllAsync(Guid? vendorId = null, VendorDebtType? debtType = null, bool? isResolved = null);
    Task<List<VendorDebtLedgerDto>> GetUnresolvedByVendorIdAsync(Guid vendorId);
}
