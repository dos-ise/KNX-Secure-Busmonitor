using Knx.Falcon;
using System;
using System.Globalization;
using System.Text;

namespace KNX_Secure_Busmonitor.MAUI.Model
{
    public class Telegramm
    {
        public Telegramm(GroupEventArgs args, DateTime timeStamp)
        {
            Args = args;
            TimeStamp = timeStamp;
            DisplayNameValue = ConvertToDisplayName(args);
        }

        private string ConvertToDisplayName(GroupEventArgs args)
        {
            if (args.Value.SizeInBit < 8)
            {
                return args.Value.Value[0].ToString();
            }

            var hex = ByteArrayToString(args.Value.Value);
            var provider = CultureInfo.InvariantCulture;
            if (int.TryParse(hex, NumberStyles.HexNumber, provider, out int intValue))
            {
                if (args.Value.Value.Length == 2)
                {
                    return (intValue / 100).ToString("n2");
                }

                return intValue.ToString();
            }
            else
            {
                return Convert.ToHexString(args.Value.Value);
            }
        }

        private string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public GroupEventArgs Args { get; }

        public string GroupName { get; set; }

        public string DisplayNameValue { get; }

        public DateTime TimeStamp { get; }
    }
}
