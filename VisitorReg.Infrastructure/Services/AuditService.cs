using VisitorReg.Domain.Entities;
using VisitorReg.Infrastructure.Repositories;

namespace VisitorReg.Infrastructure.Services;

/// <summary>
/// 稽核服務
/// </summary>
public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>
    /// 記錄稽核事件
    /// </summary>
    public async Task LogAsync(
        string actor,
        string action,
        string targetType,
        string? targetId,
        string result,
        string? detail = null,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            OccurredAt = DateTime.Now,
            Actor = actor,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Result = result,
            Detail = detail,
            Ip = ip
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// 記錄成功事件
    /// </summary>
    public Task LogSuccessAsync(
        string actor,
        string action,
        string targetType,
        string? targetId = null,
        string? detail = null,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync(actor, action, targetType, targetId, "SUCCESS", detail, ip, cancellationToken);
    }

    /// <summary>
    /// 記錄失敗事件
    /// </summary>
    public Task LogFailureAsync(
        string actor,
        string action,
        string targetType,
        string? targetId = null,
        string? detail = null,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        return LogAsync(actor, action, targetType, targetId, "FAIL", detail, ip, cancellationToken);
    }
}
