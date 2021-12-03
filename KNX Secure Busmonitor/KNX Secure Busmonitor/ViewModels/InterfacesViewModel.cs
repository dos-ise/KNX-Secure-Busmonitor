using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using Knx.Falcon.Sdk;
using System.Windows.Input;
using Busmonitor.Bootstrap;
using Xamarin.Forms;
using Knx.Falcon.Discovery;

namespace Busmonitor.ViewModels
{
    public class InterfacesViewModel : ViewModelBase
    {
        private readonly Settings _settings;
        private readonly INotificationManager _manager;

        private bool _isDiscovering;
        private string _ipAddress;
        private string _gatewayName;

        public ICommand ItemSelectedCommand { get; }
        public ICommand RefreshCommand { get; }

        public InterfacesViewModel(Settings settings, INotificationManager manager)
        {
            _settings = settings;
            _manager = manager;
            DiscoveredInterfaces = new ObservableCollection<IpDeviceDiscoveryResult>();
            DiscoverInterfaces();
            ItemSelectedCommand = new Command(ItemSelectedExecute);
            RefreshCommand = new Command(RefreshCommandExecute);
            SaveGatewayCommand = new Command(SaveGatewayExecute);
            _ipAddress = "192.168.178.7";
            _gatewayName = "Interface Name";
        }

        private void RefreshCommandExecute()
        {
            if (!_isDiscovering)
            {
                DiscoverInterfaces();
            }
        }

        private void SaveGatewayExecute()
        {
            if (IPAddress.TryParse(_ipAddress, out var ip))
            {
                _settings.IP = _ipAddress;
                _settings.InterfaceName = _gatewayName;

                _manager.SendNotification("Info:", "Saved " + _settings.InterfaceName + "(" + _settings.IP + ")");
            }
        }

        private void ItemSelectedExecute(object obj)
        {
            var args = obj as SelectedItemChangedEventArgs;
            if (args?.SelectedItem is IpDeviceDiscoveryResult result)
            {
                _settings.IP = result.LocalIPAddress.ToString();
                _settings.InterfaceName = result.FriendlyName;
                _settings.SerialNumber = result.SerialNumber;
                _settings.MediumType = result.MediumType.ToString();
                _settings.MacAddress = result.MacAddress.ToString();
                _settings.IndividualAddress = result.IndividualAddress;
                _manager.SendNotification("Info:", "Saved " + _settings.InterfaceName + "(" + _settings.IP + ")");
            }
        }

        private void DiscoverInterfaces()
        {
            DiscoveredInterfaces.Clear();
            Task.Run(() =>
              {
                  IsDiscovering = true;
                  foreach (var dis in KnxBus.DiscoverIpDevices())
                  {
                      DiscoveredInterfaces.Add(dis);
                  }
                  IsDiscovering = false;
              });
        }

        public ICommand SaveGatewayCommand { get; }

        public bool IsDiscovering
        {
            get
            {
                return _isDiscovering;
            }
            set
            {
                _isDiscovering = value;
                OnPropertyChanged(nameof(IsDiscovering));
            }
        }

        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }

        public string GatewayName
        {
            get
            {
                return _gatewayName;
            }
            set
            {
                _gatewayName = value;
                OnPropertyChanged(nameof(GatewayName));
            }
        }

        public ObservableCollection<IpDeviceDiscoveryResult> DiscoveredInterfaces { get; set; }
    }
}
