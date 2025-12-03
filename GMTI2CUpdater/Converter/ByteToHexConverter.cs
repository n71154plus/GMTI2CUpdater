using System;
using System.Globalization;
using System.Windows.Data;

namespace GMTI2CUpdater
{
    public class ByteToHexConverter : IValueConverter
    {
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
