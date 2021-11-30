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

            await foreach (var ip in KnxBus.DiscoverIpDevicesAsync())
            {
                Console.WriteLine(ip);

                // convert to a connection string
                var connectionString = ip.ToConnectionString();
                Console.WriteLine(connectionString);

                // or directly to a IPTunnelingConnectorParameters object
                if (ip.Supports(ServiceFamily.Tunneling, 1))
                {
                    var connectorParameter = IpTunnelingConnectorParameters.FromDiscovery(ip);
                }
            }


            SemanticScreenReader.Announce(CounterLabel.Text);
        }
    }
}
