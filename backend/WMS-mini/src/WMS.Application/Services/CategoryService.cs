using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(ICategoryRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = _mapper.Map<Category>(dto);
            await _repo.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null) return false;

            if (await _repo.HasProductsAsync(id))
                throw new InvalidOperationException("Cannot delete category that still has products.");

            await _repo.DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var categories = await _repo.GetAllAsync();
            return _mapper.Map<List<CategoryDto>>(categories);
            
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null)
                return null;
            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category == null) return null;

            _mapper.Map(dto, category);
            await _repo.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryDto>(category);
        }
    }
}
