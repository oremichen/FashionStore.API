using FashionStore.API.Features.Products.CreateProduct;
using FashionStore.API.Features.Products.DeleteProduct;
using FashionStore.API.Features.Products.DeleteProductImage;
using FashionStore.API.Features.Products.GetProductById;
using FashionStore.API.Features.Products.GetProductBySlug;
using FashionStore.API.Features.Products.GetProductCollection;
using FashionStore.API.Features.Products.GetProductImages;
using FashionStore.API.Features.Products.GetProducts;
using FashionStore.API.Features.Products.GetRelatedProducts;
using FashionStore.API.Features.Products.GetStorefront;
using FashionStore.API.Features.Products.UpdateProduct;

namespace FashionStore.API.Features.Products;

[Route("api/products")]
[ApiController]
[Authorize(Roles = "SuperAdmin,BusinessAdmin")]
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
    IDeleteProductImageService deleteProductImageService) : BaseApiController
{
    #region User product calls

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStorefront([FromQuery] StorefrontProductQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getStorefrontService.ExecuteAsync(query, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("featured", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("new-arrivals")]
    public async Task<IActionResult> GetNewArrivals([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("new-arrivals", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getProductCollectionService.ExecuteAsync("on-sale", page, pageSize, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{productId}/related")]
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

    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateProductForm form, CancellationToken cancellationToken)
    {
        var images = await ProductImageReader.ReadAsync(form.Images, cancellationToken);
        var request = new CreateProductRequest
        {
            CategoryId = form.CategoryId, 
            BrandId = form.BrandId, 
            Name = form.Name, 
            Slug = form.Slug,
            Description = form.Description, 
            AdditionalInformation = form.AdditionalInformation,
            ShortDescription = form.ShortDescription, 
            OldPrice = form.OldPrice, 
            NewPrice = form.NewPrice,
            CurrencyCode = form.CurrencyCode, 
            AvailabilityCount = form.AvailabilityCount, 
            Weight = form.Weight,
            WeightUnit = form.WeightUnit, 
            IsFeatured = form.IsFeatured, 
            IsNewArrival = form.IsNewArrival,
            Sizes = form.Sizes,
            Colors = form.Colors,
            Status = form.Status, 
            ImageRequests = images
        };
        return ProcessResponse(await createProductService.ExecuteAsync(request, cancellationToken));
    }

    [HttpPut("update")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Update([FromForm] UpdateProductForm form, CancellationToken cancellationToken)
    {
        var images = await ProductImageReader.ReadAsync(form.Images, cancellationToken);
        var request = new UpdateProductRequest
        {
            ProductId = form.ProductId, 
            CategoryId = form.CategoryId, 
            BrandId = form.BrandId, 
            Name = form.Name,
            Slug = form.Slug, 
            Description = form.Description, 
            AdditionalInformation = form.AdditionalInformation,
            ShortDescription = form.ShortDescription, 
            OldPrice = form.OldPrice, 
            NewPrice = form.NewPrice,
            CurrencyCode = form.CurrencyCode, 
            AvailabilityCount = form.AvailabilityCount, 
            Weight = form.Weight,
            WeightUnit = form.WeightUnit, 
            IsFeatured = form.IsFeatured, 
            IsNewArrival = form.IsNewArrival,
            Sizes = form.Sizes,
            Colors = form.Colors,
            Status = form.Status, 
            ImageRequests = images
        };
        return ProcessResponse(await updateProductService.ExecuteAsync(request, cancellationToken));
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
