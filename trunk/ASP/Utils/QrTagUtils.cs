using ASP.BaseCommon;

namespace ASP.Utils
{
    public static class QrTagUtils
    {
        private const string _sCode = "TWPIS";
        private const int _qrLength = 26; // chuoi min

        public static string GetStringFormatFromQR(string qrmsg, int format)
        {
            if (string.IsNullOrEmpty(qrmsg)) return "";
            if (qrmsg.Length < _qrLength) return "QR không hợp lệ";
            switch (format)
            {
                case (int)EnumInventoryQrFormat.SystemCode:
                    return qrmsg.Substring(0, 5);
                case (int)EnumInventoryQrFormat.ProcessCode:
                    return qrmsg.Substring(11, 10).Trim();
                case (int)EnumInventoryQrFormat.Group:
                    return qrmsg.Substring(29, 27).Trim();
                default:
                    return "";
            }
        }

        public static int GetIntFormatFromQR(string qrmsg, int format)
        {
            if (string.IsNullOrEmpty(qrmsg)) return 0;
            if (qrmsg.Length < _qrLength) return 0;
            string msgPart = "";
            switch (format)
            {
                case (int)EnumInventoryQrFormat.InventoryMonth:
                    msgPart = qrmsg.Substring(5, 2);
                    break;
                case (int)EnumInventoryQrFormat.InventoryYear:
                    msgPart = qrmsg.Substring(7, 4);
                    break;
                case (int)EnumInventoryQrFormat.TagNo:
                    msgPart = qrmsg.Substring(21, 5);//2122232425
                    break;
                case (int)EnumInventoryQrFormat.Location:
                    msgPart = qrmsg.Substring(26, 3);
                    break;
                default:
                    msgPart = "";
                    break;
            }
            return ConvertStringToInt(msgPart);
        }

        public static bool IsPastTime(int imonth, int iyear, DateTime td)
        {
            if (iyear < td.Year) return true;
            if (imonth < td.Month) return true;
            return false;
        }

        public static bool IsSystemCode(string qrmsg)
        {
            var code = GetStringFormatFromQR(qrmsg, (int)EnumInventoryQrFormat.SystemCode);
            if (code.Equals(_sCode)) return true;
            return false;
        }

        public static bool IsPhysicalInventoryTime(int month, List<int> configMonth)
        {
            if (configMonth.Contains(month)) return true;
            return false;
        }

        private static int ConvertStringToInt(string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
