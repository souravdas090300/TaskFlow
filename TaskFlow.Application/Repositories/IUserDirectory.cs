namespace TaskFlow.Application.Repositories;

public record UserContact(string Email, string FullName);

public interface IUserDirectory
{
    Task<UserContact?> GetContactAsync(Guid userId);
}
