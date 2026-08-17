using FashionStore.Application.Abstractions.PromotionBanners;

namespace FashionStore.Application.Features.PromotionBanners;

public sealed class PromotionBannerService(IPromotionBannerRepository repository) : IPromotionBannerService
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
            banner.SetImage(request.ImageData, request.ImageContentType, request.ImageFileName);
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
                banner.SetImage(request.ImageData, request.ImageContentType ?? string.Empty, request.ImageFileName ?? string.Empty);
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
        return new ResponseResult().Success("Promotion banner deleted successfully.");
    }

    public async Task<PromotionBannerImageResponse?> GetImageAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var banner = await repository.GetByIdAsync(id.Trim(), false, cancellationToken);
        return banner is null ? null : new(banner.ImageData, banner.ImageContentType);
    }

    private static PromotionBannerResponse Map(PromotionBanner banner)
    {
        return new PromotionBannerResponse
        {
            Id = banner.Id, Title = banner.Title, Subtitle = banner.Subtitle,
            Image = $"/api/promotion-banners/{banner.Id}/image", DestinationUrl = banner.DestinationUrl,
            Placement = banner.Placement, Slot = banner.Slot, IsActive = banner.IsActive
        };
    }
}
