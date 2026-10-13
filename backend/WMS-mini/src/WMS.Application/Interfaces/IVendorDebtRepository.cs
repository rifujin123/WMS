using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IVendorDebtRepository
{
    Task<List<VendorDebtLedger>> GetAllAsync(Guid? vendorId = null, VendorDebtType? debtType = null, bool? isResolved = null);
    Task<List<VendorDebtLedger>> GetUnresolvedByVendorIdAsync(Guid vendorId);
    Task<List<VendorDebtLedger>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<List<VendorDebtLedger>> GetByResolvedInPoIdAsync(Guid poId);
    Task<VendorDebtLedger?> GetByIdAsync(Guid id);
    Task AddAsync(VendorDebtLedger debt);
    Task AddRangeAsync(IEnumerable<VendorDebtLedger> debts);
    Task UpdateAsync(VendorDebtLedger debt);
}
