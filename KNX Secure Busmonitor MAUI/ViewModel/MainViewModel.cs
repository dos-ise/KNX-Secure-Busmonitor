using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KNX_Secure_Busmonitor_MAUI.Model;
using Knx.Falcon.Logging;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    [INotifyPropertyChanged]
    internal partial class MainViewModel
    {
        private KnxBus _bus;

        public MainViewModel()
        {
            Logger.Factory = new MauiLoggerFactory();
            Telegrams = new ObservableCollection<Telegram>();
            WriteValue = "False";
            TargetWriteAddress = "1/1/1";
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

        [ObservableProperty]
        private string targetWriteAddress;

        [ObservableProperty]
        private string writeValue;

        [RelayCommand]
        private void Refresh()
        {
        }

        [RelayCommand]
        private async Task OnConnect()
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

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private void Write()
        {
            try
            {
                _bus.WriteGroupValue(new GroupAddress(TargetWriteAddress), GroupValue.Parse(WriteValue));
            }
            catch (Exception e)
            {
                //TODO
            }
        }

        private bool CanWrite()
        {
            //TODO
            return true;
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
