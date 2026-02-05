using Microsoft.EntityFrameworkCore;
using VisitorReg.Domain.Entities;
using VisitorReg.Infrastructure.Data;

namespace VisitorReg.Infrastructure.Repositories;

/// <summary>
/// 稽核日誌資料存取實作
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly VisitorDbContext _context;

    public AuditLogRepository(VisitorDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? actor,
        string? action,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        // 日期區間篩選
        if (startDate.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= endDate.Value);
        }

        // 操作者篩選
        if (!string.IsNullOrWhiteSpace(actor))
        {
            query = query.Where(a => a.Actor.Contains(actor));
        }

        // 動作篩選
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        // 總筆數
        var totalCount = await query.CountAsync(cancellationToken);

        // 分頁與排序
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
