using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace FashionStore.Application.Dtos.Request
{

    public sealed class CreateBrandForm
    {
        [Required, StringLength(150)] public string Name { get; init; } = string.Empty;
        [Required, StringLength(180)] public string Slug { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? WebsiteUrl { get; init; }
        public bool IsActive { get; init; } = true;
        public IFormFile? Image { get; init; }
    }

}
