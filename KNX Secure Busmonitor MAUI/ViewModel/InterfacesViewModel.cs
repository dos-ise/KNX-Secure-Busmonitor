using CommunityToolkit.Mvvm.ComponentModel;
using Knx.Falcon.Sdk;
using Knx.Falcon.Discovery;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using KNX_Secure_Busmonitor_MAUI.Model;
using System.Net;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    [INotifyPropertyChanged]
    internal partial class InterfacesViewModel
    {
        public InterfacesViewModel()
        {
            DiscoveredInterfaces = new ObservableCollection<IpDeviceDiscoveryResult>();
            DiscoverInterfaces();
            PropertyChanged += InterfacesViewModel_PropertyChanged;
            IpAddress = "192.168.178.46";
        }

        private void InterfacesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(SelectedInterface)))
            {
                Preferences.Default.Set(MonitorPreferences.IpAddress, SelectedInterface.DiscoveryEndpoint.Address.MapToIPv4().ToString());            
            }
        }

        [ObservableProperty]
        private ObservableCollection<IpDeviceDiscoveryResult> discoveredInterfaces;

        [ObservableProperty]
        private IpDeviceDiscoveryResult selectedInterface;

        [ObservableProperty]
        private bool isDiscovering;

        [ObservableProperty]
        private string ipAddress;

        [RelayCommand]
        private void Save()
        {
            if(IPAddress.TryParse(IpAddress, out var parsedAddress))
            {
                Preferences.Default.Set(MonitorPreferences.IpAddress, parsedAddress.MapToIPv4().ToString());
            }
        }

        [RelayCommand]
        private void DiscoverInterfaces()
        {
            DiscoveredInterfaces.Clear();
            Task.Run(() =>
            {
                IsDiscovering = true;
                foreach (var dis in KnxBus.DiscoverIpDevices())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DiscoveredInterfaces.Add(dis);
                    });
                }
                
                IsDiscovering = false;
            });
        }
    }
}
