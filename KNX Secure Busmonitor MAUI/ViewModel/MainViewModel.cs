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
        private KnxBus _bus;

        public MainViewModel()
        {
            Telegrams = new ObservableCollection<Telegram>();

            string ip = KnxBus.DiscoverIpDevices().ToList().FirstOrDefault()?.LocalIPAddress.MapToIPv4().ToString();
            ip = "192.168.178.46";
            Preferences.Default.Set(MonitorPreferences.IpAddress, ip);
        }

        private ConnectorParameters CreateParameter()
        {
            string ip = Preferences.Default.Get(MonitorPreferences.IpAddress, string.Empty);
            return new IpTunnelingConnectorParameters(ip);
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
            _bus = new KnxBus(CreateParameter());

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Telegrams.Add(new Telegram()
                {
                    Name = "TODO",
                    DestinationAddress = e.DestinationAddress,
                    SourceAddress = e.SourceAddress,
                    Value = e.Value.ToString()
                });
            });
        }
    }
}
