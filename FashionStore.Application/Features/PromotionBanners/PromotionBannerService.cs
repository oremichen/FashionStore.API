using FashionStore.Application.Abstractions.PromotionBanners;
using FashionStore.Application.Abstractions.Images;

namespace FashionStore.Application.Features.PromotionBanners;

public sealed class PromotionBannerService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : IPromotionBannerService
{
    public async Task<ResponseResult<IReadOnlyList<PromotionBannerResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var banners = await repository.GetAllAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<PromotionBannerResponse>>().Success(banners.Select(Map).ToList(), "Promotion banners retrieved successfully.");
    }

    public async Task<ResponseResult<PromotionBannerResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionBannerResponse>();
        if (string.IsNullOrWhiteSpace(id)) return response.Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return banner is null ? response.Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD) : response.Success(Map(banner));
    }

    public async Task<ResponseResult<PromotionBannerResponse>> CreateAsync(CreatePromotionBannerRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionBannerResponse>();
        if (await repository.SlotExistsAsync(request.Slot, null, cancellationToken))
            return response.Fail($"Slot {request.Slot} is already in use.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            var banner = PromotionBanner.Create(request.Title, request.Subtitle, request.DestinationUrl, request.Placement, request.Slot, request.IsActive);
            var upload = await cloudinary.UploadWithMetadataAsync(request.ImageData, request.ImageFileName, cancellationToken);
            banner.SetImageUrl(upload.Url, upload.ContentType, upload.FileName, upload.FileSize);
            await repository.AddAsync(banner, cancellationToken);
            return response.Success(Map(banner), "Promotion banner created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult<PromotionBannerResponse>> UpdateAsync(string id, UpdatePromotionBannerRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionBannerResponse>();
        if (string.IsNullOrWhiteSpace(id)) return response.Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (banner is null) return response.Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.SlotExistsAsync(request.Slot, banner.Id, cancellationToken))
            return response.Fail($"Slot {request.Slot} is already in use.", ResponseCodes.DUPLICATE_RECORD);
        try
        {
            banner.SetDetails(request.Title, request.Subtitle, request.DestinationUrl, request.Placement, request.Slot, request.IsActive);
            if (request.ImageData is { Length: > 0 })
            {
                var oldUrl = banner.ImageUrl;
                var upload = await cloudinary.UploadWithMetadataAsync(request.ImageData, request.ImageFileName ?? "promotion-banner", cancellationToken);
                banner.SetImageUrl(upload.Url, upload.ContentType, upload.FileName, upload.FileSize);
                await cloudinary.DeleteAsync(oldUrl, cancellationToken);
            }
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(Map(banner), "Promotion banner updated successfully.");
        }
        catch (ArgumentException exception) { return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION); }
    }

    public async Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return new ResponseResult().Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (banner is null) return new ResponseResult().Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        await repository.DeleteAsync(banner, cancellationToken);
        await cloudinary.DeleteAsync(banner.ImageUrl, cancellationToken);
        return new ResponseResult().Success("Promotion banner deleted successfully.");
    }

    private static PromotionBannerResponse Map(PromotionBanner banner)
    {
        return new PromotionBannerResponse
        {
            Id = banner.Id, Title = banner.Title, Subtitle = banner.Subtitle,
            Image = banner.ImageUrl ?? string.Empty, DestinationUrl = banner.DestinationUrl,
            Placement = banner.Placement, Slot = banner.Slot, IsActive = banner.IsActive
        };
    }
}
