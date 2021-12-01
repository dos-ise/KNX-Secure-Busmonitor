using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using System;

namespace KNX_Secure_Busmonitor.MAUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterLabel.Text = $"Current count: {count}";

            var parameter = new IpTunnelingConnectorParameters("192.168.178.46");
            try
            {
                var connection = new KnxBus(parameter);
                connection.ConnectionStateChanged += _connection_OnStateChange;
                connection.GroupMessageReceived += _connection_OnData;
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


            SemanticScreenReader.Announce(CounterLabel.Text);
        }

        private void _connection_OnData(object sender, GroupEventArgs e)
        {

        }

        private void _connection_OnStateChange(object sender, EventArgs e)
        {

        }
    }
}
