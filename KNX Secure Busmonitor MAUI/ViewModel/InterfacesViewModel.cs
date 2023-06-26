using CommunityToolkit.Mvvm.ComponentModel;
using Knx.Falcon.Sdk;
using Knx.Falcon.Discovery;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using KNX_Secure_Busmonitor_MAUI.Model;

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
