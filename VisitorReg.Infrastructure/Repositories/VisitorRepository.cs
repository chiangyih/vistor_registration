using Microsoft.EntityFrameworkCore;
using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;
using VisitorReg.Infrastructure.Data;

namespace VisitorReg.Infrastructure.Repositories;

/// <summary>
/// 訪客資料存取實作
/// </summary>
public class VisitorRepository : IVisitorRepository
{
    private readonly VisitorDbContext _context;

    public VisitorRepository(VisitorDbContext context)
    {
        _context = context;
    }

    public async Task<Visitor> AddAsync(Visitor visitor, CancellationToken cancellationToken = default)
    {
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync(cancellationToken);
        return visitor;
    }

    public async Task<Visitor> UpdateAsync(Visitor visitor, CancellationToken cancellationToken = default)
    {
        _context.Visitors.Update(visitor);
        await _context.SaveChangesAsync(cancellationToken);
        return visitor;
    }

    public async Task<Visitor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Visitors
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Visitor?> GetByRegisterNoAsync(string registerNo, CancellationToken cancellationToken = default)
    {
        return await _context.Visitors
            .FirstOrDefaultAsync(v => v.RegisterNo == registerNo, cancellationToken);
    }

    public async Task<(List<Visitor> Items, int TotalCount)> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? name,
        string? company,
        string? hostName,
        VisitorStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Visitors.AsQueryable();

        // 日期區間篩選
        if (startDate.HasValue)
        {
            query = query.Where(v => v.CheckInAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(v => v.CheckInAt <= endDate.Value);
        }

        // 姓名篩選
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(v => v.Name.Contains(name));
        }

        // 公司篩選
        if (!string.IsNullOrWhiteSpace(company))
        {
            query = query.Where(v => v.Company != null && v.Company.Contains(company));
        }

        // 受訪者篩選
        if (!string.IsNullOrWhiteSpace(hostName))
        {
            query = query.Where(v => v.HostName.Contains(hostName));
        }

        // 狀態篩選
        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        // 總筆數
        var totalCount = await query.CountAsync(cancellationToken);

        // 分頁與排序
        var items = await query
            .OrderByDescending(v => v.CheckInAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<string> GenerateRegisterNoAsync(CancellationToken cancellationToken = default)
    {
        // 格式：V + 日期 (yyyyMMdd) + 流水號 (4碼)
        var today = DateTime.Today;
        var prefix = $"V{today:yyyyMMdd}";

        // 取得今日最後一筆登記編號
        var lastRegisterNo = await _context.Visitors
            .Where(v => v.RegisterNo.StartsWith(prefix))
            .OrderByDescending(v => v.RegisterNo)
            .Select(v => v.RegisterNo)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (!string.IsNullOrEmpty(lastRegisterNo) && lastRegisterNo.Length >= 12)
        {
            var lastSequence = lastRegisterNo[^4..];
            if (int.TryParse(lastSequence, out var num))
            {
                sequence = num + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }
}
