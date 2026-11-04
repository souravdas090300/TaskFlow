using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public class AddressService
{
    private readonly ApplicationDbContext _context;

    public AddressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AddressDto>> GetAddressesAsync(Guid userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.IsDefault ? 0 : 1)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Type = a.Type,
                FirstName = a.FirstName,
                LastName = a.LastName,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                Phone = a.Phone,
                IsDefault = a.IsDefault,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AddressDto?> GetAddressByIdAsync(Guid userId, Guid addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null) return null;

        return new AddressDto
        {
            Id = address.Id,
            UserId = address.UserId,
            Type = address.Type,
            FirstName = address.FirstName,
            LastName = address.LastName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            Phone = address.Phone,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt
        };
    }

    public async Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressDto dto)
    {
        // If this is set as default, remove default from other addresses of same type
        if (dto.IsDefault)
        {
            var existingDefaults = await _context.Addresses
                .Where(a => a.UserId == userId && a.Type == dto.Type && a.IsDefault)
                .ToListAsync();

            foreach (var existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
            }
        }

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = dto.Type,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            Phone = dto.Phone,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        var result = await GetAddressByIdAsync(userId, address.Id);
        return result ?? throw new InvalidOperationException("Failed to retrieve created address");
    }

    public async Task<AddressDto?> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto)
    {
        var addr = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (addr == null) return null;

        // If this is set as default, remove default from other addresses of same type
        if (dto.IsDefault && !addr.IsDefault)
        {
            var existingDefaults = await _context.Addresses
                .Where(a => a.UserId == userId && a.Type == dto.Type && a.Id != addressId && a.IsDefault)
                .ToListAsync();

            foreach (var existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
            }
        }

        // PUT is a full replacement - all fields are required
        addr.Type = dto.Type;
        addr.FirstName = dto.FirstName;
        addr.LastName = dto.LastName;
        addr.AddressLine1 = dto.AddressLine1;
        addr.AddressLine2 = dto.AddressLine2;
        addr.City = dto.City;
        addr.State = dto.State;
        addr.PostalCode = dto.PostalCode;
        addr.Country = dto.Country;
        addr.Phone = dto.Phone;
        addr.IsDefault = dto.IsDefault;
        addr.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetAddressByIdAsync(userId, addressId);
    }

    public async Task<AddressDto?> PatchAddressAsync(Guid userId, Guid addressId, PatchAddressDto dto)
    {
        var addr = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (addr == null) return null;

        // PATCH is partial update - only update provided fields (non-null and non-empty for strings)
        if (!string.IsNullOrEmpty(dto.Type)) addr.Type = dto.Type;
        if (!string.IsNullOrEmpty(dto.FirstName)) addr.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName)) addr.LastName = dto.LastName;
        if (!string.IsNullOrEmpty(dto.AddressLine1)) addr.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 is not null) addr.AddressLine2 = dto.AddressLine2; // Allow empty/null for optional field
        if (!string.IsNullOrEmpty(dto.City)) addr.City = dto.City;
        if (dto.State is not null) addr.State = dto.State; // Allow empty for optional state
        if (!string.IsNullOrEmpty(dto.PostalCode)) addr.PostalCode = dto.PostalCode;
        if (!string.IsNullOrEmpty(dto.Country)) addr.Country = dto.Country;
        if (dto.Phone is not null) addr.Phone = dto.Phone; // Allow empty/null for optional field
        if (dto.IsDefault.HasValue) addr.IsDefault = dto.IsDefault.Value;
        addr.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetAddressByIdAsync(userId, addressId);
    }

    public async Task<bool> DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var addr = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (addr == null) return false;

        _context.Addresses.Remove(addr);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AddressDto?> GetDefaultAddressAsync(Guid userId, string type)
    {
        var addr = await _context.Addresses
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == type && a.IsDefault);

        if (addr == null) return null;

        return new AddressDto
        {
            Id = addr.Id,
            UserId = addr.UserId,
            Type = addr.Type,
            FirstName = addr.FirstName,
            LastName = addr.LastName,
            AddressLine1 = addr.AddressLine1,
            AddressLine2 = addr.AddressLine2,
            City = addr.City,
            State = addr.State,
            PostalCode = addr.PostalCode,
            Country = addr.Country,
            Phone = addr.Phone,
            IsDefault = addr.IsDefault,
            CreatedAt = addr.CreatedAt
        };
    }
}
