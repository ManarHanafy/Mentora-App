namespace api.Contracts.Users;

public record RoleResponse(string Role, IReadOnlyList<string> Permissions);
