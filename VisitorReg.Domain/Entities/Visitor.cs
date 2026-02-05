using VisitorReg.Domain.Enums;

namespace VisitorReg.Domain.Entities;

/// <summary>
/// 訪客實體
/// </summary>
public class Visitor
{
    /// <summary>
    /// 主鍵 (GUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 登記編號 (可人讀)
    /// </summary>
    public string RegisterNo { get; set; } = string.Empty;

    /// <summary>
    /// 訪客姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 證件號遮罩 (如 A12****789)
    /// </summary>
    public string? IdNumberMasked { get; set; }

    /// <summary>
    /// 公司/單位
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// 來訪目的
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// 受訪者
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// 到訪時間
    /// </summary>
    public DateTime CheckInAt { get; set; }

    /// <summary>
    /// 離場時間
    /// </summary>
    public DateTime? CheckOutAt { get; set; }

    /// <summary>
    /// 聯絡方式 (可選)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 狀態 (0=在場, 1=已離場, 2=作廢)
    /// </summary>
    public VisitorStatus Status { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 建立者
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新者
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 併發控制版本
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// 檢查是否可以離場
    /// </summary>
    public bool CanCheckout()
    {
        return Status == VisitorStatus.InSite;
    }

    /// <summary>
    /// 執行離場
    /// </summary>
    public void Checkout(DateTime checkoutTime, string updatedBy)
    {
        if (!CanCheckout())
        {
            throw new InvalidOperationException("訪客已離場或已作廢，無法再次離場");
        }

        CheckOutAt = checkoutTime;
        Status = VisitorStatus.CheckedOut;
        UpdatedAt = DateTime.Now;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// 作廢
    /// </summary>
    public void Void(string updatedBy)
    {
        Status = VisitorStatus.Voided;
        UpdatedAt = DateTime.Now;
        UpdatedBy = updatedBy;
    }
}
