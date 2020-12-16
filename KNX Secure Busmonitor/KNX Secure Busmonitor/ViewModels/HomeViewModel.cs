using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Busmonitor.Model;
using Busmonitor.Views;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Plugin.LocalNotifications;
using Xamarin.Forms;
using Device = Xamarin.Forms.Device;

namespace Busmonitor.ViewModels
{
  public class HomeViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private KnxBus _bus;

    private string targetWriteAddress;
    private string writeValue;

    private bool _isConnecting;

    public HomeViewModel(Settings settings, TelegrammList telegrammList)
    {
      _settings = settings;
      _bus = new KnxBus(CreateParameter());
      Telegramms = telegrammList;
      ConnectCommand = new Command(OnConnect);
      WriteCommand = new Command(OnWrite);
      TargetWriteAddress = "1/1/1";
      WriteValue = "true";
    }

    public ICommand ConnectCommand { get; }

    public ICommand WriteCommand { get; }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    public Telegramm SelectedTelegramm { get; set; }

    public string ConnectButtonText => _bus.ConnectionState == BusConnectionState.Connected ? "Disconnect" : "Connect";

    public Color ConnectButtonColor => _bus.ConnectionState == BusConnectionState.Connected ? Color.GreenYellow : Color.Red;

    public bool IsConnected => _bus.ConnectionState == BusConnectionState.Connected;

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
    private async void OnWrite()
    {
      try
      {
        await _bus.WriteGroupValueAsync(new GroupAddress(TargetWriteAddress), GroupValue.Parse(WriteValue));
      }
      catch (Exception e)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            CrossLocalNotifications.Current.Show("Error:", "Could not write to " + TargetWriteAddress + "(" + e.Message + ")");
          });
      }
    }

    private async void OnConnect()
    {
      ConnectorParameters connectorParameter = CreateParameter();
      if (_bus.ConnectionState != BusConnectionState.Connected)
      {
        if (!_isConnecting)
        {
          await Task.Run(() => Action(connectorParameter));
        }
      }
      else
      {
        //_bus.Disconnect();
        Telegramms.Clear();
        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(IsConnected));
      }
    }

    private void Action(ConnectorParameters connectorParameter)
    {
      _bus = new KnxBus(connectorParameter);

      try
      {
        _isConnecting = true;
        _bus.ConnectAsync();

        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(IsConnected));
      }
      catch (Exception ex)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            CrossLocalNotifications.Current.Show("Error:", "Could not connect to " + _settings.InterfaceName + "(" + _settings.IP + ")" + ex.Message);
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
            CrossLocalNotifications.Current.Show("Info", "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
          });

        var senderAddress = _bus.InterfaceConfiguration.DomainAddress;
        _bus.GroupMessageReceived += (sender, args) =>
        {
          Device.BeginInvokeOnMainThread(() =>
          {
            var gaName = FindGroupName(args);
            var t = new Telegramm(args, DateTime.Now) {GroupName = gaName};
            Telegramms.Add(t);
            OnPropertyChanged(nameof(Telegramms));
            HomeView.ScrollToBottom();
          });
        };
        while (_bus.ConnectionState == BusConnectionState.Connected)
        {
        }
      }
    }

    private string FindGroupName(GroupEventArgs arg)
    {
      var ga = _settings.ImportGroupAddress?.FirstOrDefault(me => me.Address == arg.DestinationAddress.Address);
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
        return new IpTunnelingConnectorParameters(_settings.IP, _settings.IpPort);
      }
      else
      {
        return new IpTunnelingConnectorParameters(_settings.IP, _settings.IpPort);
      }
    }
  }
}
