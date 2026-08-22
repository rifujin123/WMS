using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(ICustomerRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await _repo.GetAllAsync();
        return _mapper.Map<List<CustomerDto>>(customers);
    }

    public Task<PagedResult<CustomerDto>> GetPagedAsync(
        CustomerListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer == null) return null;
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var name = dto.Name.Trim();
        if (await _repo.ExistsByNameAsync(name))
            throw new InvalidOperationException($"Khách hàng '{name}' đã tồn tại.");

        var customer = _mapper.Map<Customer>(dto);
        customer.Name = name;
        await _repo.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer == null) return null;

        var name = dto.Name.Trim();
        if (await _repo.ExistsByNameAsync(name, excludeId: id))
            throw new InvalidOperationException($"Khách hàng '{name}' đã tồn tại.");

        _mapper.Map(dto, customer);
        customer.Name = name;
        await _repo.UpdateAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer == null) return false;
        await _repo.DeleteAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}