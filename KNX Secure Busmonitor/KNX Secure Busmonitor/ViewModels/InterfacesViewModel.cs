using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Knx.Bus.Common.KnxIp;
using Knx.Falcon.Sdk;

namespace Busmonitor.ViewModels
{
  using System.Windows.Input;

  using Plugin.LocalNotifications;

  using Xamarin.Forms;

  public class InterfacesViewModel : INotifyPropertyChanged
  {
    private readonly Settings _settings;

    private bool _isDiscovering;

    public ICommand ItemSelectedCommand { get; }

    public InterfacesViewModel(Settings settings)
    {
      _settings = settings;
      DiscoveredInterfaces = new ObservableCollection<DiscoveryResult>();
      Networks = new ObservableCollection<NetworkAdapterInfo>(new NetworkAdapterEnumerator(AdapterTypes.All));
      DiscoverInterfaces();
      ItemSelectedCommand = new Command(ItemSelectedExecute);
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

        CrossLocalNotifications.Current.Show("Info:", "Saved " + _settings.InterfaceName + "(" + _settings.IP + ")");
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

    public ObservableCollection<DiscoveryResult> DiscoveredInterfaces { get; set; }

    public ObservableCollection<NetworkAdapterInfo> Networks { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
