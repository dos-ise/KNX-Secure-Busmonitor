using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Input;
using Knx.Falcon.Discovery;
using Plugin.LocalNotifications;

using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  public class InterfacesViewModel : ViewModelBase
  {
    private readonly Settings _settings;

    private bool _isDiscovering;

    public ICommand ItemSelectedCommand { get; }

    public InterfacesViewModel(Settings settings)
    {
      _settings = settings;
      DiscoveredInterfaces = new ObservableCollection<IpDeviceDiscoveryResult>();
      //Networks = new ObservableCollection<NetworkAdapterInfo>(new NetworkAdapterEnumerator(AdapterTypes.All));
      DiscoverInterfaces();
      ItemSelectedCommand = new Command(ItemSelectedExecute);
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

        CrossLocalNotifications.Current.Show("Info:", "Saved " + _settings.InterfaceName + "(" + _settings.IP + ")");
      }
    }

    private void DiscoverInterfaces()
    {
      Task.Run(() =>
        {
          IsDiscovering = true;
          var devices = new IpDeviceDiscovery().DiscoverAsync(new CancellationToken()).ToListAsync().Result;
          foreach (var dis in devices)
          {
            DiscoveredInterfaces.Add(dis);
          }
          IsDiscovering = false;
        });
    }

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

    public ObservableCollection<IpDeviceDiscoveryResult> DiscoveredInterfaces { get; set; }
  }
}
