using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

using Knx.Bus.Common.KnxIp;
using Knx.Falcon.Sdk;
using System.Windows.Input;
using Busmonitor.Bootstrap;
using Xamarin.Forms;

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

    public InterfacesViewModel(Settings settings, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      DiscoveredInterfaces = new ObservableCollection<DiscoveryResult>();
      Networks = new ObservableCollection<NetworkAdapterInfo>(new NetworkAdapterEnumerator(AdapterTypes.All));
      DiscoverInterfaces();
      ItemSelectedCommand = new Command(ItemSelectedExecute);
      SaveGatewayCommand = new Command(SaveGatewayExecute);
      _ipAddress = "192.168.178.7";
      _gatewayName = "Interface Name";
    }

    private void SaveGatewayExecute()
    {
      _settings.IP = _ipAddress;
      _settings.InterfaceName = _gatewayName;

      _manager.SendNotification("Info:", "Saved " + _settings.InterfaceName + "(" + _settings.IP + ")");
    }

    private void ItemSelectedExecute(object obj)
    {
      var args = obj as SelectedItemChangedEventArgs;
      if (args?.SelectedItem is DiscoveryResult result)
      {
        _settings.IP = result.IpAddress.ToString();
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
      Task.Run(() =>
        {
          IsDiscovering = true;
          foreach (var dis in new DiscoveryClient(AdapterTypes.All).Discover())
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

    public ObservableCollection<DiscoveryResult> DiscoveredInterfaces { get; set; }

    public ObservableCollection<NetworkAdapterInfo> Networks { get; }
  }
}
