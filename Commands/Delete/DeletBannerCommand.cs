using API_Ecommerce.Data;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    public class DeleteBannerCommand : IRequest<bool>
    {
        public long Id { get; set; }

        public DeleteBannerCommand(long id)
        {
            Id = id;
        }
    }

    public class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeleteBannerCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<bool> Handle(DeleteBannerCommand request, CancellationToken cancellationToken)
        {
            var banner = await _context.Banners
                .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

            if (banner == null)
            {
                return false;
            }

            // Optional: Delete the image file from wwwroot if it exists
            if (!string.IsNullOrEmpty(banner.ImageUrl))
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRootPath, banner.ImageUrl.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}