using Busmonitor.Bootstrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KNX_Secure_Busmonitor.Tizen.TV.Services
{
    public class TizenNotificationManager : INotificationManager
    {
        public event EventHandler NotificationReceived;
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void SendNotification(string title, string message, DateTime? notifyTime = null)
        {
            throw new NotImplementedException();
        }

        public void ReceiveNotification(string title, string message)
        {
            throw new NotImplementedException();
        }
    }
}
