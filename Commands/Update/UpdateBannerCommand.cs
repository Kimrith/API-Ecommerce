using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class UpdateBannerCommand : IRequest<BannerResponseDto?>
    {
        public long Id { get; set; }
        public UpdateBannerDto Dto { get; set; }

        public UpdateBannerCommand(long id, UpdateBannerDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }

    public class UpdateBannerCommandHandler : IRequestHandler<UpdateBannerCommand, BannerResponseDto?>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UpdateBannerCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<BannerResponseDto?> Handle(UpdateBannerCommand request, CancellationToken cancellationToken)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

            if (banner == null)
            {
                return null;
            }

            var dto = request.Dto;

            // Handle new image upload if provided
            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Delete old image file if it exists
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var oldFilePath = Path.Combine(webRootPath, banner.ImageUrl.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Save new image
                banner.ImageUrl = await SaveImageAsync(dto.Image);
            }

            // Update fields
            banner.Title = dto.Title;
            banner.Subtitle = dto.Subtitle;
            banner.TargetUrl = dto.TargetUrl;
            banner.Position = dto.Position;
            banner.DisplayOrder = dto.DisplayOrder;
            banner.IsActive = dto.IsActive;
            banner.StartsAt = dto.StartsAt;
            banner.ExpiresAt = dto.ExpiresAt;

            _context.Banners.Update(banner);
            await _context.SaveChangesAsync(cancellationToken);

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