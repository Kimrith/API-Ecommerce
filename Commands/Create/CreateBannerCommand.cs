using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;

namespace API_Ecommerce.Commands.Create
{
    public class CreateBannerCommand : IRequest<BannerResponseDto>
    {
        public CreateBannerDto Dto { get; set; }

        public CreateBannerCommand(CreateBannerDto dto)
        {
            Dto = dto;
        }
    }

    public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, BannerResponseDto>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateBannerCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<BannerResponseDto> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Handle Image Upload
            string imageUrl = string.Empty;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                imageUrl = await SaveImageAsync(dto.Image);
            }

            // 2. Map DTO to Entity
            var banner = new Banner
            {
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                ImageUrl = imageUrl,
                TargetUrl = dto.TargetUrl,
                Position = dto.Position,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive,
                StartsAt = dto.StartsAt,
                ExpiresAt = dto.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Save to Database
            _context.Banners.Add(banner);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Return Response DTO
            return new BannerResponseDto
            {
                Id = banner.Id,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                ImageUrl = banner.ImageUrl,
                TargetUrl = banner.TargetUrl,
                Position = banner.Position,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive,
                StartsAt = banner.StartsAt,
                ExpiresAt = banner.ExpiresAt,
                CreatedAt = banner.CreatedAt
            };
        }

        // --- Helper: Image Saver ---
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "banners");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/banners/{uniqueFileName}";
        }
    }
}