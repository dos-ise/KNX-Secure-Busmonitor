using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KNX_Secure_Busmonitor.MAUI.Model;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            Discover();
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

        private readonly IPAddress _targetAddress = IpRoutingConnectorParameters.SystemSetupMulticastAddress;
        private byte[] _buffer;

        private void Discover()
        {
            try
            {
                var a = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (Exception ex)
            {

            }

            var networks = GetNetworks().ToList();
            var _client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.ExclusiveAddressUse = false;
            _client.MulticastLoopback = true;

            //var mcOpt = new MulticastOption(_multicastEndPoint.Address, new IPEndPoint(localIpAddress.IPAddress, 0).Address);
            //_client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcOpt);

            //EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            //_client.BeginReceiveFrom(_buffer, 0, MaxReceiveSize, SocketFlags.None, ref remoteEndPoint, ReceiveCompleted, this);
        }

        private IEnumerable<(IPAddress IPAddress, NetworkInterface NetworkInterface)> GetNetworks()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(_ => _.OperationalStatus == OperationalStatus.Up)
                .SelectMany(a => a.GetIPProperties().UnicastAddresses.Select(_ => _.Address)
                    .Where(_ => _.AddressFamily == AddressFamily.InterNetwork).Select(i => (i, a)));
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
