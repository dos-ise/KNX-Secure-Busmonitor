using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Knx.Falcon;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel()
        {
            try
            {
                var discovered = KnxBus.DiscoverIpDevices().ToList();
                var parameter = new IpTunnelingConnectorParameters("192.168.178.46");
                var connection = new KnxBus(parameter);
                connection.ConnectionStateChanged += OnConnectionStateChanged;
                connection.GroupMessageReceived += OnGroupMessageReceived;
                connection.Connect();
            }
            catch (Exception ex)
            {
                var x = ex.Message;
            }
        }

        private void OnConnectionStateChanged(object sender, EventArgs e)
        {
        }

        private void OnGroupMessageReceived(object sender, GroupEventArgs e)
        {
        }
    }
}
