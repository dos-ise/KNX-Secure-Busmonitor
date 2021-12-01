using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KNX_Secure_Busmonitor.MAUI.Model;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNX_Secure_Busmonitor.MAUI
{
    public class MainPageViewModel
    {
        public MainPageViewModel()
        {
            ConnectCommand = new Command(Connect);
            Telegramms = new ObservableCollection<Telegramm>();
        }

        public Command ConnectCommand { get; }

        public ObservableCollection<Telegramm> Telegramms { get; }

        public void Connect()
        {
            var parameter = new IpTunnelingConnectorParameters("192.168.178.46");
            try
            {
                var connection = new KnxBus(parameter);
                connection.ConnectionStateChanged += OnConnectionStateChanged;
                connection.GroupMessageReceived += OnGroupMessageReceived;
                connection.Connect();
            }
            catch (Exception ex)
            {

            }

            //TODO discovery does not seem to work
            //await foreach (var ip in KnxBus.DiscoverIpDevicesAsync())
            //{
            //    Console.WriteLine(ip);

            //    // convert to a connection string
            //    var connectionString = ip.ToConnectionString();
            //    Console.WriteLine(connectionString);

            //    // or directly to a IPTunnelingConnectorParameters object
            //    if (ip.Supports(ServiceFamily.Tunneling, 1))
            //    {
            //        var connectorParameter = IpTunnelingConnectorParameters.FromDiscovery(ip);
            //    }
            //}
        }

        private void OnGroupMessageReceived(object sender, GroupEventArgs e)
        {
            Telegramms.Add(new Telegramm(e, DateTime.UtcNow));
        }

        private void OnConnectionStateChanged(object sender, EventArgs e)
        {
        }
    }
}
