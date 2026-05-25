namespace api.Contracts.Users;

public record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password
);
