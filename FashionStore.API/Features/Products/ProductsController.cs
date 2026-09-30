using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.DeleteProduct;
using FashionStore.API.Features.Products.DeleteProductImage;
using FashionStore.API.Features.Products.GetProductById;
using FashionStore.API.Features.Products.GetProductBySlug;
using FashionStore.API.Features.Products.GetProductCollection;
using FashionStore.API.Features.Products.GetProductImages;
using FashionStore.API.Features.Products.GetProductVarient;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetRelatedProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;
using FashionStore.API.Features.Products.Shared;
using FashionStore.API.Features.Products.UploadProductImage;

namespace FashionStore.API.Features.Products;

[Route("api/products")]
[ApiController]
[Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
public sealed class ProductsController(
    IGetStorefrontService getStorefrontService,
    IGetProductCollectionService getProductCollectionService,
    IGetProductBySlugService getProductBySlugService,
    IGetRelatedProductsService getRelatedProductsService,
    IGetProductsService getProductsService,
    IGetProductByIdService getProductByIdService,
    ICreateProductService createProductService,
    IUpdateProductService updateProductService,
    IDeleteProductService deleteProductService,
    IGetProductImagesService getProductImagesService,
    IGetProductVarientService getProductVarientService,
    IDeleteProductImageService deleteProductImageService,
    IProductUploadService productUploadService,
    ILogger<ProductsController> logger) : BaseApiController
{
    #region User product calls

    [AllowAnonymous]
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.ProductListing)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStorefront([FromQuery] StorefrontProductQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getStorefrontService.ExecuteAsync(query, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("featured")]
    [EnableRateLimiting(RateLimitPolicies.ProductListing)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeatured([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("featured", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("new-arrivals")]
    [EnableRateLimiting(RateLimitPolicies.ProductListing)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNewArrivals([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("new-arrivals", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("on-sale")]
    [EnableRateLimiting(RateLimitPolicies.ProductListing)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOnSale([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("on-sale", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{productId}/related")]
    [EnableRateLimiting(RateLimitPolicies.ProductListing)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelated(string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getRelatedProductsService.ExecuteAsync(productId, page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{productSlug}")]
    [ProducesResponseType(typeof(ResponseResult<ProductDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string productSlug, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getProductBySlugService.ExecuteAsync(productSlug, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{productId}/variants")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductVariantResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductVariantResponse>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVariants(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getProductVarientService.ExecuteAsync(productId, cancellationToken));
    }

    #endregion

    #region Admin product calls

    [HttpGet("~/api/admin/products")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getProductsService.ExecuteAsync(query, cancellationToken));
    }

    [HttpGet("~/api/admin/products/{productId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getProductByIdService.ExecuteAsync(productId, cancellationToken));
    }
  
    [HttpPost("uploads")]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("application/octet-stream", "image/jpeg", "image/png", "image/webp", "image/gif", "image/avif")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductImageUploadResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product image upload started. Method: {Method}, Path: {Path}, ContentType: {ContentType}, ContentLength: {ContentLength}, BodyType: {BodyType}, CanRead: {CanRead}, CanSeek: {CanSeek}",
            Request.Method,
            Request.Path,
            Request.ContentType,
            Request.ContentLength,
            Request.Body.GetType().FullName,
            Request.Body.CanRead,
            Request.Body.CanSeek);

        var fileName = Request.Headers["X-File-Name"].FirstOrDefault()
            ?? Request.Query["fileName"].FirstOrDefault();

        logger.LogInformation("Product image upload filename resolved. FileName: {FileName}", fileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ProcessResponse(new ResponseResult().Fail(
                "X-File-Name is required.",
                ResponseCodes.INVALID_ACTION));
        }

        var contentType = Request.ContentType?.Split(';', 2)[0].Trim() ?? string.Empty;
        logger.LogInformation(
            "Product image upload request body ready. FileName: {FileName}, ContentType: {ContentType}, DeclaredLength: {ContentLength}",
            fileName,
            contentType,
            Request.ContentLength);

        var upload = await productUploadService.UploadAsync(
            Request.Body,
            contentType,
            fileName,
            Request.ContentLength,
            cancellationToken);

        logger.LogInformation(
            "Product image upload completed. UploadId: {UploadId}, FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}, ExpiresAt: {ExpiresAt}",
            upload.UploadId,
            upload.FileName,
            upload.ContentType,
            upload.FileSize,
            upload.ExpiresAt);

        return ProcessResponse(
            new ResponseResult<ProductImageUploadResponse>()
                .Success(upload, "Image uploaded successfully.")
                .SetStatusCode(ResponseCodes.CREATED));
    }

    [HttpPost("create-product")]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductJsonRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating product with request: {@Request}", request);
        var images = await productUploadService.TakeImagesAsync(
            request.ImageUploadIds,
            cancellationToken);

        if (images.Count == 0)
        {
            logger.LogError("No images provided for product creation. ProductName: {ProductName}", request.Name);
            return ProcessResponse(new ResponseResult().Fail(
                "At least one image is required to create a product.",
                ResponseCodes.INVALID_ACTION));
        }

        var createRequest = new CreateProductRequest
        {
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            TypeId = request.TypeId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            AdditionalInformation = request.AdditionalInformation,
            ShortDescription = request.ShortDescription,
            OldPrice = request.OldPrice,
            NewPrice = request.NewPrice,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            IsOldNewPrice = request.IsOldNewPrice,
            IsMinMaxPrice = request.IsMinMaxPrice,
            CurrencyCode = request.CurrencyCode,
            AvailabilityCount = request.AvailabilityCount,
            Weight = request.Weight,
            WeightUnit = request.WeightUnit,
            IsFeatured = request.IsFeatured,
            IsNewArrival = request.IsNewArrival,
            Sizes = request.Sizes,
            Colors = request.Colors,
            Status = request.Status,
            ProductVariants = request.ProductVariants,
            ImageRequests = images
        };

        return ProcessResponse(await createProductService.ExecuteAsync(createRequest, cancellationToken));
    }


    [HttpPut("update-product")]
    [EnableRateLimiting(RateLimitPolicies.AdminUpload)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(
        [FromBody] UpdateProductJsonRequest request,
        CancellationToken cancellationToken)
    {
        var images = await productUploadService.TakeImagesAsync(
            request.ImageUploadIds,
            cancellationToken);

        var updateRequest = new UpdateProductRequest
        {
            ProductId = request.ProductId,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            TypeId = request.TypeId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            AdditionalInformation = request.AdditionalInformation,
            ShortDescription = request.ShortDescription,
            OldPrice = request.OldPrice,
            NewPrice = request.NewPrice,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            IsOldNewPrice = request.IsOldNewPrice,
            IsMinMaxPrice = request.IsMinMaxPrice,
            CurrencyCode = request.CurrencyCode,
            AvailabilityCount = request.AvailabilityCount,
            Weight = request.Weight,
            WeightUnit = request.WeightUnit,
            IsFeatured = request.IsFeatured,
            IsNewArrival = request.IsNewArrival,
            Sizes = request.Sizes,
            Colors = request.Colors,
            Status = request.Status,
            ProductVariants = request.ProductVariants,
            ImageRequests = images
        };

        return ProcessResponse(await updateProductService.ExecuteAsync(updateRequest, cancellationToken));
    }

    [HttpDelete("{productId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteProductService.ExecuteAsync(productId, cancellationToken));
    }

    [HttpGet("{productId}/images")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductImageResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductImageResponse>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetImages(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getProductImagesService.ExecuteAsync(productId, cancellationToken));
    }

    [HttpDelete("{productId}/images/{imageId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteImage(string productId, string imageId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteProductImageService.ExecuteAsync(productId, imageId, cancellationToken));
    }

    #endregion

}
