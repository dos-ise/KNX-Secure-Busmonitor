using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Input;
using Busmonitor.Bootstrap;
using Busmonitor.Extension;
using Busmonitor.Model;
using Busmonitor.Views;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using Xamarin.Forms;
using Device = Xamarin.Forms.Device;

namespace Busmonitor.ViewModels
{
    public class HomeViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private readonly INotificationManager _manager;
    private KnxBus _bus;

    private string targetWriteAddress;
    private string writeValue;

    private bool _isConnecting;
    
    public HomeViewModel(Settings settings, TelegrammList telegrammList, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      _settings.PropertyChanged += SettingsOnPropertyChanged;
      Knx.Falcon.Logging.Logger.Factory = new MyLoggerFactory();
      _bus = new KnxBus(CreateParameter());
      Telegramms = telegrammList;
      ConnectCommand = new Command(OnConnect);
      WriteCommand = new Command(OnWrite);
      TargetWriteAddress = "01/1/001";
      WriteValue = "true";
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName.Equals(nameof(Settings.IP)))
      {
        OnPropertyChanged(nameof(NoGateway));
        OnPropertyChanged(nameof(GatewaySelected));
      }
    }

    public ICommand ConnectCommand { get; }

    public ICommand WriteCommand { get; }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    public Telegramm SelectedTelegramm { get; set; }

    public string ConnectButtonText => _bus.ConnectionState == Knx.Falcon.BusConnectionState.Connected ? "Disconnect" : "Connect";

    public Color ConnectButtonColor => _bus.ConnectionState == Knx.Falcon.BusConnectionState.Connected ? Color.GreenYellow : Color.Red;

    public bool IsConnected => _bus.ConnectionState == Knx.Falcon.BusConnectionState.Connected;

    public bool NoGateway => _settings.IP == null;

    public bool GatewaySelected => !NoGateway;

    public string TargetWriteAddress
    {
      get => targetWriteAddress;
      set
      {
        targetWriteAddress = value;
        OnPropertyChanged(nameof(TargetWriteAddress));
      }
    }

    public string WriteValue
    {
      get => writeValue;
      set
      {
        writeValue = value;
        OnPropertyChanged(nameof(WriteValue));
      }
    }

    private void OnOpenWrite()
    {

    }
    private void OnWrite()
    {
      try
      {
        _bus.WriteGroupValue(new GroupAddress(TargetWriteAddress), GroupValue.Parse(WriteValue));
      }
      catch (Exception e)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            _manager.SendNotification("Error:", "Could not write to " + TargetWriteAddress + "(" + e.Message + ")");
          });
      }
    }

    private async void OnConnect()
    {
      try
      {
        var connectorParameter = CreateParameter();
        if (_bus.ConnectionState != BusConnectionState.Connected)
        {
          if (!_isConnecting)
          {
            await Action(connectorParameter);
          }
        }
        else
        {
          await _bus.DisposeAsync();
          Telegramms.Clear();
          OnPropertyChanged(nameof(ConnectButtonColor));
          OnPropertyChanged(nameof(ConnectButtonText));
          OnPropertyChanged(nameof(IsConnected));
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
    }

    private async Task Action(ConnectorParameters connectorParameter)
    {
      _bus = new KnxBus(connectorParameter);

      try
      {
        _isConnecting = true;
        await _bus.ConnectAsync();

        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(IsConnected));
      }
      catch (Exception ex)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            _manager.SendNotification("Error:", "Could not connect to " + _settings.InterfaceName + "(" + _settings.IP + ")" + ex.Message);
          });

        return;
      }
      finally
      {
        _isConnecting = false;
      }

      if (_bus.ConnectionState == BusConnectionState.Connected)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            _manager.SendNotification("Info", "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
          });

        var senderAddress = "";
                _bus.GroupMessageReceived += _bus_GroupMessageReceived;
 
        while (_bus.ConnectionState == BusConnectionState.Connected)
        {
        }
      }
    }

        private void _bus_GroupMessageReceived(object sender, GroupEventArgs args)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var gaName = FindGroupName(args);
                var t = new Telegramm(args, DateTime.Now) { GroupName = gaName };
                Telegramms.Add(t);
                OnPropertyChanged(nameof(Telegramms));
                HomeView.ScrollToBottom();
            });
        }

        private string FindGroupName(GroupEventArgs arg)
    {
      var ga = _settings.ImportGroupAddress?.FirstOrDefault(me => me.Address == arg.DestinationAddress);
      return ga == null ? string.Empty : ga.GroupName;
    }

    private ConnectorParameters CreateParameter()
    {
      if (_settings.IsSecurityEnabled)
      {
        try
        {
            //SecureString password = _settings.Password.ToSecureString();
            //var secure = new KnxIpSecureTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
            //secure.IndividualAddress = _settings.IndividualAddress;
            //secure.LoadSecurityData(_settings.KnxKeys.ToStream(), password);
            //return secure;
            return null;
        }
        catch (Exception e)
        {
          return new IpTunnelingConnectorParameters(_settings.IP);
        }
      }
      else
      {
        return new IpTunnelingConnectorParameters(_settings.IP, protocolType: IpProtocol.Udp);
      }
    }
  }
}
