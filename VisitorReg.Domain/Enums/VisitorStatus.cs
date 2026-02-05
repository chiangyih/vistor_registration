namespace VisitorReg.Domain.Enums;

/// <summary>
/// 訪客狀態
/// </summary>
public enum VisitorStatus : byte
{
    /// <summary>
    /// 在場
    /// </summary>
    InSite = 0,

    /// <summary>
    /// 已離場
    /// </summary>
    CheckedOut = 1,

    /// <summary>
    /// 作廢
    /// </summary>
    Voided = 2
}
