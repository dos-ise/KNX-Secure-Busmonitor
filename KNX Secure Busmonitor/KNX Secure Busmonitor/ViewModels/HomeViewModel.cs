using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

using Busmonitor.Model;

using Knx.Bus.Common;
using Knx.Bus.Common.Configuration;
using Knx.Bus.Common.GroupValues;
using Knx.Falcon.Sdk;

using Plugin.LocalNotifications;

using Xamarin.Forms;

using Device = Xamarin.Forms.Device;
using Busmonitor.Views;

namespace Busmonitor.ViewModels
{
  public class HomeViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private Bus _bus;

    private string targetWriteAddress;
    private string writeValue;

    private bool _isConnecting;

    public HomeViewModel(Settings settings)
    {
      _settings = settings;
      _bus = new Bus(CreateParameter());
      Telegramms = new ObservableCollection<Telegramm>();
      ConnectCommand = new Command(OnConnect);
      WriteCommand = new Command(OnWrite);
      TargetWriteAddress = "1/1/1";
      WriteValue = "true";
    }

    public HomeViewModel Home => this;

    public InterfacesViewModel Interface => new InterfacesViewModel(_settings);

    public SecurtiyViewModel Security => new SecurtiyViewModel();

    public ExportViewModel Export => new ExportViewModel(_settings);

    public GroupAddressImportViewModel GaImport => new GroupAddressImportViewModel(_settings);

    public Settings Settings => _settings;

    public ICommand ConnectCommand { get; }

    public ICommand WriteCommand { get; }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    public Telegramm SelectedTelegramm { get; set; }

    public string ConnectButtonText => _bus.IsConnected ? "Disconnect" : "Connect";

    public Color ConnectButtonColor => _bus.IsConnected ? Color.GreenYellow : Color.Red;

    public bool IsConnected => this._bus.IsConnected;

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
      _bus.WriteValue(new GroupAddress(TargetWriteAddress), GroupValue.Parse(WriteValue));
    }

    private async void OnConnect()
    {
      ConnectorParameters connectorParameter = CreateParameter();
      if (!_bus.IsConnected)
      {
        if (!_isConnecting)
        {
          await Task.Run(() => Action(connectorParameter));
        }
      }
      else
      {
        _bus.Disconnect();
        Telegramms.Clear();
        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(IsConnected));
      }
    }

    private void Action(ConnectorParameters connectorParameter)
    {
      _bus = new Bus(connectorParameter);

      try
      {
        _isConnecting = true;
        _bus.Connect();

        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(IsConnected));
      }
      catch (Exception exception)
      {
        Device.BeginInvokeOnMainThread(
          () =>
            {
              CrossLocalNotifications.Current.Show("Error:", "Could not connect to " + _settings.InterfaceName + "(" + _settings.IP + ")");
            });

        return;
      }
      finally
      {
        _isConnecting = false;
      }

      if (_bus.IsConnected)
      {
        Device.BeginInvokeOnMainThread(
          () =>
            {
              CrossLocalNotifications.Current.Show("Info", "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
            });

        var senderAddress = _bus.LocalIndividualAddress;
        _bus.GroupValueReceived += args =>
          {
            Device.BeginInvokeOnMainThread(() =>
              {
                var gaName = FindGroupName(args);
                var t = new Telegramm(args, DateTime.Now);
                t.GroupName = gaName;
                Telegramms.Add(t);
                OnPropertyChanged(nameof(Telegramms));
                HomePage.ScrollToBottom();
              });
          };

        while (_bus.IsConnected)
        {
        }
      }
    }

    private string FindGroupName(GroupValueEventArgs arg)
    {
      var ga = _settings.ImportGroupAddress?.FirstOrDefault(me => me.Address == arg.Address.Address);
      return ga == null ? string.Empty : ga.GroupName;
    }

    private ConnectorParameters CreateParameter()
    {
      if (_settings.IsSecurityEnabled)
      {
        //TODO
        //var secure = new KnxIpSecureDeviceManagementConnectorParameters(SelectedInterface);
        //secure.LoadSecurityData(SelectedInterface.IndividualAddress, _fileName, MakeStringSecure(passwordEntry.Text));
        //return secure;
        return new KnxIpTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
      }
      else
      {
        return new KnxIpTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
      }
    }
  }
}
