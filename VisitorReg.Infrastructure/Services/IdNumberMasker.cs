namespace VisitorReg.Infrastructure.Services;

/// <summary>
/// 證件號碼遮罩工具
/// </summary>
public static class IdNumberMasker
{
    /// <summary>
    /// 將證件號碼遮罩處理
    /// 保留前3碼和後3碼，中間以星號取代
    /// </summary>
    /// <param name="idNumber">原始證件號碼</param>
    /// <returns>遮罩後的證件號碼</returns>
    public static string? Mask(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return null;
        }

        // 移除空白
        var trimmed = idNumber.Trim();

        // 如果長度小於等於6，全部遮罩（保留第一個字元）
        if (trimmed.Length <= 6)
        {
            return trimmed.Length > 0 
                ? trimmed[0] + new string('*', trimmed.Length - 1) 
                : trimmed;
        }

        // 保留前3碼和後3碼
        var prefix = trimmed.Substring(0, 3);
        var suffix = trimmed.Substring(trimmed.Length - 3);
        var maskLength = trimmed.Length - 6;

        return $"{prefix}{new string('*', maskLength)}{suffix}";
    }

    /// <summary>
    /// 驗證證件號碼格式（台灣身分證格式）
    /// </summary>
    /// <param name="idNumber">證件號碼</param>
    /// <returns>是否為有效格式</returns>
    public static bool IsValidTaiwanId(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return false;
        }

        var trimmed = idNumber.Trim().ToUpper();

        // 台灣身分證：1個英文字母 + 9個數字
        if (trimmed.Length != 10)
        {
            return false;
        }

        if (!char.IsLetter(trimmed[0]))
        {
            return false;
        }

        for (int i = 1; i < 10; i++)
        {
            if (!char.IsDigit(trimmed[i]))
            {
                return false;
            }
        }

        return true;
    }
}
