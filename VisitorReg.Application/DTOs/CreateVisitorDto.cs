namespace VisitorReg.Application.DTOs;

/// <summary>
/// 建立訪客 DTO
/// </summary>
public class CreateVisitorDto
{
    /// <summary>
    /// 訪客姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 證件號碼 (將被遮罩)
    /// </summary>
    public string? IdNumber { get; set; }

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
    public DateTime CheckInAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 聯絡方式
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    public string? Note { get; set; }
}
