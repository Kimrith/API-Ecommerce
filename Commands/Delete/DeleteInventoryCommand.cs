using API_Ecommerce.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    public class DeleteInventoryCommand : IRequest<bool>
    {
        public long Id { get; set; }

        public DeleteInventoryCommand(long id)
        {
            Id = id;
        }
    }

    public class DeleteInventoryCommandHandler : IRequestHandler<DeleteInventoryCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteInventoryCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteInventoryCommand request, CancellationToken cancellationToken)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (inventory == null)
            {
                return false;
            }

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}