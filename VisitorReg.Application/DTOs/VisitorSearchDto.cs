using VisitorReg.Domain.Enums;

namespace VisitorReg.Application.DTOs;

/// <summary>
/// 訪客查詢條件 DTO
/// </summary>
public class VisitorSearchDto
{
    /// <summary>
    /// 開始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 結束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 訪客姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 公司/單位
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// 受訪者
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// 狀態
    /// </summary>
    public VisitorStatus? Status { get; set; }

    /// <summary>
    /// 頁碼 (從 0 開始)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; } = 20;
}
