namespace api.Infrastructure.Audit;

/// <summary>Service for logging authorization and audit events.</summary>
public interface IAuditLogger
{
    /// <summary>Log an authorization failure.</summary>
    Task LogAuthorizationFailureAsync(int userId, string resource, string action, string reason);

    /// <summary>Log a permission change.</summary>
    Task LogPermissionChangeAsync(int adminId, int targetUserId, string oldRole, string newRole);

    /// <summary>Log an access to sensitive resource.</summary>
    Task LogSensitiveAccessAsync(int userId, string resourceType, int resourceId);
}

/// <summary>Default implementation of audit logger using ILogger.</summary>
public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    private static readonly EventId AuthorizationFailureEvent = new(5001, "AuthorizationFailure");
    private static readonly EventId PermissionChangeEvent = new(5002, "PermissionChange");
    private static readonly EventId SensitiveAccessEvent = new(5003, "SensitiveAccess");

    public Task LogAuthorizationFailureAsync(int userId, string resource, string action, string reason)
    {
        logger.LogWarning(
            AuthorizationFailureEvent,
            "AuditEvent={AuditEvent} UserId={UserId} Action={Action} Resource={Resource} Reason={Reason}",
            "authorization_failure", userId, action, resource, reason);
        return Task.CompletedTask;
    }

    public Task LogPermissionChangeAsync(int adminId, int targetUserId, string oldRole, string newRole)
    {
        logger.LogInformation(
            PermissionChangeEvent,
            "AuditEvent={AuditEvent} AdminId={AdminId} TargetUserId={TargetUserId} OldRole={OldRole} NewRole={NewRole}",
            "permission_change", adminId, targetUserId, oldRole, newRole);
        return Task.CompletedTask;
    }

    public Task LogSensitiveAccessAsync(int userId, string resourceType, int resourceId)
    {
        logger.LogInformation(
            SensitiveAccessEvent,
            "AuditEvent={AuditEvent} UserId={UserId} ResourceType={ResourceType} ResourceId={ResourceId}",
            "sensitive_access", userId, resourceType, resourceId);
        return Task.CompletedTask;
    }
}
