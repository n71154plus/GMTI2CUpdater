using System;
using System.Collections.Generic;
using System.Globalization;

namespace GMTI2CUpdater.Helper
{
    /// <summary>
    /// 提供十六進位解析相關的簡易工具函式。
    /// </summary>
    public static class HexHelper
    {
        /// <summary>
        /// 嘗試將傳入字串視為十六進位格式並轉換為 byte，失敗時回傳 null。
        /// </summary>
        /// <param name="s">可能含有 0x 前綴的十六進位文字。</param>
        public static byte? ParseHexByteOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Trim();

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(2);

            if (byte.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
                return result;

            return null; // 解析失敗也當成沒有定義 / 無效
        }
    }


}
