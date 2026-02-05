using VisitorReg.Domain.Enums;

namespace VisitorReg.Application.DTOs;

/// <summary>
/// 訪客資料 DTO
/// </summary>
public class VisitorDto
{
    public Guid Id { get; set; }
    public string RegisterNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IdNumberMasked { get; set; }
    public string? Company { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public string? Phone { get; set; }
    public string? Note { get; set; }
    public VisitorStatus Status { get; set; }
    public string StatusText => Status switch
    {
        VisitorStatus.InSite => "在場",
        VisitorStatus.CheckedOut => "已離場",
        VisitorStatus.Voided => "作廢",
        _ => "未知"
    };
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
