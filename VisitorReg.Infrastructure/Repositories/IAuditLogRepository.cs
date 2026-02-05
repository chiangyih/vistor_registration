using VisitorReg.Domain.Entities;

namespace VisitorReg.Infrastructure.Repositories;

/// <summary>
/// 稽核日誌資料存取介面
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// 新增稽核日誌
    /// </summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢稽核日誌 (分頁)
    /// </summary>
    Task<(List<AuditLog> Items, int TotalCount)> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? actor,
        string? action,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}
