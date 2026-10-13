using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.Application.Services;

public class VendorDebtService : IVendorDebtService
{
    private readonly IVendorDebtRepository _repo;
    private readonly IMapper _mapper;

    public VendorDebtService(IVendorDebtRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<VendorDebtLedgerDto>> GetAllAsync(
        Guid? vendorId = null,
        VendorDebtType? debtType = null,
        bool? isResolved = null)
    {
        var entities = await _repo.GetAllAsync(vendorId, debtType, isResolved);
        return _mapper.Map<List<VendorDebtLedgerDto>>(entities);
    }

    public async Task<List<VendorDebtLedgerDto>> GetUnresolvedByVendorIdAsync(Guid vendorId)
    {
        var entities = await _repo.GetUnresolvedByVendorIdAsync(vendorId);
        return _mapper.Map<List<VendorDebtLedgerDto>>(entities);
    }
}
