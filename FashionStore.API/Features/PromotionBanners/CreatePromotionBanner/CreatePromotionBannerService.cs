using FashionStore.Domain.Abstractions.PromotionBanners;
using FashionStore.Domain.Abstractions.Images;

namespace FashionStore.API.Features.PromotionBanners.CreatePromotionBanner;
public sealed class CreatePromotionBannerService(IPromotionBannerRepository repository, ICloudinaryImageService cloudinary) : ICreatePromotionBannerService
{
    public async Task<ResponseResult<PromotionBannerResponse>> ExecuteAsync(CreatePromotionBannerRequest request, CancellationToken cancellationToken)
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
