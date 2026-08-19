using AutoMapper;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IProductRepository repo, IMapper mapper, IImageService imageService, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _repo.GetAllAsync();
        return _mapper.Map<List<ProductDto>>(products);
    }

    public Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repo.GetPagedAsync(query, pageSize, cancellationToken);

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null)
            return null;
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, Stream? imageStream = null, string? imageFileName = null)
    {
        var product = _mapper.Map<Product>(dto);
        await _repo.AddAsync(product);

        if (imageStream != null && !string.IsNullOrWhiteSpace(imageFileName))
        {
            var url = await _imageService.UploadAsync(imageStream, imageFileName, $"wms/products/{product.Id}", 600, 600);
            product.ImageUrl = url;
        }

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, Stream? imageStream = null, string? imageFileName = null)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return null;

        _mapper.Map(dto, product);

        if (imageStream != null && !string.IsNullOrWhiteSpace(imageFileName))
        {
            var url = await _imageService.UploadAsync(imageStream, imageFileName, $"wms/products/{id}", 600, 600);
            product.ImageUrl = url;
        }

        await _repo.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return false;

        if (await _repo.HasReferencesAsync(id))
            throw new InvalidOperationException("Cannot delete product that is referenced by stock or orders.");

        await _repo.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<string?> UploadImageAsync(Guid id, Stream fileStream, string fileName)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product == null) return null;

        var url = await _imageService.UploadAsync(fileStream, fileName, $"wms/products/{id}", 600, 600);

        product.ImageUrl = url;
        await _repo.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return product.ImageUrl;
    }
}
