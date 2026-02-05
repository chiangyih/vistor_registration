using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;

namespace VisitorReg.Infrastructure.Repositories;

/// <summary>
/// 訪客資料存取介面
/// </summary>
public interface IVisitorRepository
{
    /// <summary>
    /// 新增訪客
    /// </summary>
    Task<Visitor> AddAsync(Visitor visitor, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新訪客
    /// </summary>
    Task<Visitor> UpdateAsync(Visitor visitor, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得訪客
    /// </summary>
    Task<Visitor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據登記編號取得訪客
    /// </summary>
    Task<Visitor?> GetByRegisterNoAsync(string registerNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢訪客 (分頁)
    /// </summary>
    Task<(List<Visitor> Items, int TotalCount)> SearchAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? name,
        string? company,
        string? hostName,
        VisitorStatus? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 產生登記編號
    /// </summary>
    Task<string> GenerateRegisterNoAsync(CancellationToken cancellationToken = default);
}
