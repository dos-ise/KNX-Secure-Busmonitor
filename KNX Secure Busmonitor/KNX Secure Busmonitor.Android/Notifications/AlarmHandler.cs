using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KNX_Secure_Busmonitor.Droid.Notifications
{
  public class AlarmHandler : BroadcastReceiver
  {
    public override void OnReceive(Context context, Intent intent)
    {
      if (intent?.Extras != null)
      {
        string title = intent.GetStringExtra(AndroidNotificationManager.TitleKey);
        string message = intent.GetStringExtra(AndroidNotificationManager.MessageKey);

        AndroidNotificationManager.Instance.Show(title, message);
      }
    }
  }
}