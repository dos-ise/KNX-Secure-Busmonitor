﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Busmonitor.Bootstrap;
using Busmonitor.Extension;
using Busmonitor.Model;
using Busmonitor.Views;
using Knx.Bus.Common;
using Knx.Bus.Common.Configuration;
using Knx.Bus.Common.GroupValues;
using Knx.Falcon.Sdk;

using Xamarin.Forms;
using Device = Xamarin.Forms.Device;

namespace Busmonitor.ViewModels
{
  public class HomeViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private readonly INotificationManager _manager;
    private Bus _bus;

    private string targetWriteAddress;
    private string writeValue;

    private bool _isConnecting;
    
    public HomeViewModel(Settings settings, TelegrammList telegrammList, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      _settings.PropertyChanged += SettingsOnPropertyChanged;
      _bus = new Bus(CreateParameter());
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

    public string ConnectButtonText => _bus.IsConnected ? "Disconnect" : "Connect";

    public Color ConnectButtonColor => _bus.IsConnected ? Color.GreenYellow : Color.Red;

    public bool IsConnected => _bus.IsConnected;

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
        _bus.WriteValue(new GroupAddress(TargetWriteAddress), GroupValue.Parse(WriteValue));
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
      catch (Exception e)
      {
        Console.WriteLine(e);
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

      if (_bus.IsConnected)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            _manager.SendNotification("Info", "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
          });

        var senderAddress = _bus.LocalIndividualAddress;
        _bus.GroupValueReceived += args =>
        {
          Device.BeginInvokeOnMainThread(() =>
          {
            var gaName = FindGroupName(args);
            var t = new Telegramm(args, DateTime.Now) { GroupName = gaName };
            Telegramms.Add(t);
            OnPropertyChanged(nameof(Telegramms));
            HomeView.ScrollToBottom();
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
        try
        {
          SecureString password = _settings.Password.ToSecureString();
          var secure = new KnxIpSecureTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
          secure.IndividualAddress = _settings.IndividualAddress;
          secure.LoadSecurityData(_settings.KnxKeys.ToStream(), password);
          return secure;
        }
        catch (Exception e)
        {
          return new KnxIpTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
        }
      }
      else
      {
        return new KnxIpTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
      }
    }
  }
}
