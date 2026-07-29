using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Application.Services
{
    public class ReceivingService : IReceivingService
    {
        private readonly IReceivingRepository _repo;
        private readonly IPurchaseOrderRepository _poRepo;
        private readonly IMapper _mapper;

        public ReceivingService(IReceivingRepository repo, IPurchaseOrderRepository poRepo, IMapper mapper)
        {
            _repo = repo;
            _poRepo = poRepo;
            _mapper = mapper;
        }

        public async Task<List<ReceivingDto>> GetAllAsync()
        {
            var receivings = await _repo.GetAllAsync();
            return _mapper.Map<List<ReceivingDto>>(receivings);
        }

        public async Task<ReceivingDto?> GetByIdAsync(Guid id)
        {
            var receiving = await _repo.GetByIdAsync(id);
            if (receiving == null) return null;
            return _mapper.Map<ReceivingDto>(receiving);
        }

        public async Task<ReceivingDto?> CreateAsync(CreateReceivingDto dto)
        {
            var purchaseOrder = await _poRepo.GetByIdAsync(dto.PurchaseOrderId);
            if (purchaseOrder == null) return null;
            if (purchaseOrder.Status != PurchaseOrderStatus.Approved) return null;

            var receiving = new Receiving
            {
                PurchaseOrderId = dto.PurchaseOrderId,
                ReceivedDate = DateTime.UtcNow,
                Status = ReceivingStatus.Draft,
                Notes = dto.Notes
            };

            foreach (var detailDto in dto.ReceivingDetails)
            {
                var poDetail = purchaseOrder.PurchaseOrderDetails
                    .FirstOrDefault(d => d.ProductId == detailDto.ProductId);

                receiving.ReceivingDetails.Add(new ReceivingDetail
                {
                    ProductId = detailDto.ProductId,
                    ExpectedQuantity = poDetail?.OrderedQuantity ?? 0,
                    ActualQuantity = detailDto.ActualQuantity,
                    Condition = detailDto.Condition
                });
            }

            await _repo.AddAsync(receiving);
            return _mapper.Map<ReceivingDto>(receiving);
        }

        public async Task<ReceivingDto?> ConfirmAsync(Guid id)
        {
            var receiving = await _repo.GetByIdAsync(id);
            if (receiving == null) return null;
            if (receiving.Status != ReceivingStatus.Draft) return null;

            receiving.Status = ReceivingStatus.Confirmed;
            receiving.PurchaseOrder.Status = PurchaseOrderStatus.Received;

            await _repo.UpdateAsync(receiving);
            return _mapper.Map<ReceivingDto>(receiving);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var receiving = await _repo.GetByIdAsync(id);
            if (receiving == null) return false;
            if (receiving.Status != ReceivingStatus.Draft) return false;

            await _repo.DeleteAsync(receiving);
            return true;
        }
    }
}
