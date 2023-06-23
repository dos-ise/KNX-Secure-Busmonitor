using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNX_Secure_Busmonitor_MAUI.Model
{
    internal class Telegram
    {
        public string Name { get; set; }
        public string DestinationAddress { get; set; }
        public string SourceAddress { get; set; }
        public string Value { get; set; }
    }
}
