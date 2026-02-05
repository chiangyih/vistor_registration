namespace VisitorReg.Domain.Entities;

/// <summary>
/// 稽核日誌實體
/// </summary>
public class AuditLog
{
    /// <summary>
    /// 主鍵
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 事件發生時間
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// 操作者 (帳號)
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// 動作類型 (CREATE/UPDATE/DELETE/EXPORT/LOGIN等)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 目標類型 (Visitors/Users/Settings)
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 目標主鍵
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 結果 (SUCCESS/FAIL)
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 事件細節 (避免存放敏感原文)
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// IP 位址 (IPv4/IPv6)
    /// </summary>
    public string? Ip { get; set; }
}
