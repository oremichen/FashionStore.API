using FashionStore.Domain.Abstractions.PromotionBanners;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.PromotionBanners.UpdatePromotionBanner;
public sealed class UpdatePromotionBannerService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : IUpdatePromotionBannerService
{
    public async Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(string id, UpdatePromotionBannerRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PromotionBannerResponse>();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Promotion banner id is required.", ResponseCodes.INVALID_ACTION);
        var banner = await repository.GetByIdAsync(id.Trim(), true, cancellationToken);
        if (banner is null)
            return response.Fail("Promotion banner was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
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
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static PromotionBannerResponse Map(PromotionBanner banner)
    {
        return new PromotionBannerResponse
        {
            Id = banner.Id,
            Title = banner.Title,
            Subtitle = banner.Subtitle,
            Image = banner.ImageUrl ?? string.Empty,
            DestinationUrl = banner.DestinationUrl,
            Placement = banner.Placement,
            Slot = banner.Slot,
            IsActive = banner.IsActive
        };
    }
}
