using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KNX_Secure_Busmonitor_MAUI.Model;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    [INotifyPropertyChanged]
    internal partial class MainViewModel
    {
        private readonly KnxBus _bus;

        public MainViewModel()
        {
            _bus = new KnxBus(CreateParameter());
            Telegrams = new ObservableCollection<Telegram>();
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
            //Telegrams.Add(new Telegram() { Name = "Test", DestinationAddress = "1/1/1", Value = "true" });
        }

        private ConnectorParameters CreateParameter()
        {
            ////TODO Test
            var ip = KnxBus.DiscoverIpDevices().ToList().FirstOrDefault()?.LocalIPAddress;
            return new IpTunnelingConnectorParameters(ip?.MapToIPv4().ToString());
        }

        [ObservableProperty]
        private ObservableCollection<Telegram> telegrams;

        [ObservableProperty]
        private Telegram selectedTelegram;

        [ObservableProperty]
        private bool isRefreshing;


        [RelayCommand]
        private void Refresh()
        {
            Console.WriteLine("Hello!");
        }

        [RelayCommand]
        private async void OnConnect()
        {
            try
            {
                if (_bus.ConnectionState != BusConnectionState.Connected)
                {
                    await _bus.ConnectAsync();
                }

                _bus.GroupMessageReceived += _bus_GroupMessageReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void _bus_GroupMessageReceived(object sender, GroupEventArgs e)
        {
            Telegrams.Add(new Telegram() 
            { 
                Name = "TODO", 
                DestinationAddress = e.DestinationAddress, 
                SourceAddress = e.SourceAddress, 
                Value = e.Value.ToString()
            });
        }
    }
}
