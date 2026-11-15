namespace TaskFlow.Application.Repositories;

public interface IAddressLookup
{
    /// <summary>True only if the address exists AND belongs to this user.</summary>
    Task<bool> ExistsForUserAsync(Guid addressId, Guid userId);
}
