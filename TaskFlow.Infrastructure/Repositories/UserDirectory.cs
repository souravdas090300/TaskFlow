using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.Infrastructure.Repositories;

public class UserDirectory : IUserDirectory
{
    private readonly ApplicationDbContext _context;

    public UserDirectory(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserContact?> GetContactAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        return new UserContact(user.Email, $"{user.FirstName} {user.LastName}");
    }
}
