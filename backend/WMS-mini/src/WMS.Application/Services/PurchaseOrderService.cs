using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IMapper _mapper;

        public PurchaseOrderService(IPurchaseOrderRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<PurchaseOrderDto>> GetAllAsync()
        {
            var purchaseOrders = await _repo.GetAllAsync();
            return _mapper.Map<List<PurchaseOrderDto>>(purchaseOrders);
        }

        public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
        {
            var purchaseOrder = await _repo.GetByIdAsync(id);
            if (purchaseOrder == null) return null;
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto, Guid userId)
        {
            var purchaseOrder = _mapper.Map<PurchaseOrder>(dto);
            purchaseOrder.Status = PurchaseOrderStatus.Pending;
            purchaseOrder.CreatedById = userId;

            await _repo.AddAsync(purchaseOrder);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<PurchaseOrderDto?> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto)
        {
            var purchaseOrder = await _repo.GetByIdAsync(id);
            if (purchaseOrder == null) return null;
            if (purchaseOrder.Status != PurchaseOrderStatus.Pending) return null;

            purchaseOrder.VendorName = dto.VendorName;

            // Xóa detail cũ rồi thay bằng danh sách mới — tránh sót dòng cũ trong DB
            await _repo.RemoveDetailsAsync(purchaseOrder.Id);
            purchaseOrder.PurchaseOrderDetails = _mapper.Map<List<PurchaseOrderDetail>>(dto.PurchaseOrderDetails);
            foreach (var detail in purchaseOrder.PurchaseOrderDetails)
                detail.PurchaseOrderId = purchaseOrder.Id;

            await _repo.UpdateAsync(purchaseOrder);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var purchaseOrder = await _repo.GetByIdAsync(id);
            if (purchaseOrder == null) return false;
            if (purchaseOrder.Status != PurchaseOrderStatus.Pending) return false;

            await _repo.DeleteAsync(purchaseOrder);
            return true;
        }

        public async Task<PurchaseOrderDto?> ApproveAsync(Guid id)
        {
            var purchaseOrder = await _repo.GetByIdAsync(id);
            if (purchaseOrder == null) return null;
            if (purchaseOrder.Status != PurchaseOrderStatus.Pending) return null;

            purchaseOrder.Status = PurchaseOrderStatus.Approved;
            await _repo.UpdateAsync(purchaseOrder);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }

        public async Task<PurchaseOrderDto?> CloseAsync(Guid id)
        {
            var purchaseOrder = await _repo.GetByIdAsync(id);
            if (purchaseOrder == null) return null;
            if (purchaseOrder.Status != PurchaseOrderStatus.Received) return null;

            purchaseOrder.Status = PurchaseOrderStatus.Closed;
            await _repo.UpdateAsync(purchaseOrder);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }
    }
}
