using System;
using System.Globalization;
using System.Windows.Data;

namespace GMTI2CUpdater
{
    /// <summary>
    /// 將 byte 或 int 轉換成 0xXX 形式的十六進位文字，並支援反向解析。
    /// </summary>
    public class ByteToHexConverter : IValueConverter
    {
        /// <summary>
        /// 由數值轉換成顯示用的十六進位字串，超出範圍時回傳 "??"。
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte b)
            {
                return $"0x{b:X2}";
            }

            if (value is int i && i >= 0 && i <= 255)
            {
                return $"0x{i:X2}";
            }

            return "??";
        }

        /// <summary>
        /// 從文字轉回 byte，無法解析時維持原值不變。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                s = s.Trim();

                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    s = s.Substring(2);

                if (byte.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                    return b;
            }

            return Binding.DoNothing;
        }
    }
}
