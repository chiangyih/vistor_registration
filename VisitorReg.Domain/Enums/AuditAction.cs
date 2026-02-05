namespace VisitorReg.Domain.Enums;

/// <summary>
/// 稽核動作類型
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// 建立
    /// </summary>
    Create,

    /// <summary>
    /// 更新
    /// </summary>
    Update,

    /// <summary>
    /// 刪除
    /// </summary>
    Delete,

    /// <summary>
    /// 作廢
    /// </summary>
    Void,

    /// <summary>
    /// 匯出
    /// </summary>
    Export,

    /// <summary>
    /// 登入
    /// </summary>
    Login,

    /// <summary>
    /// 登出
    /// </summary>
    Logout,

    /// <summary>
    /// 查詢
    /// </summary>
    Query,

    /// <summary>
    /// 設定變更
    /// </summary>
    SettingChange
}
