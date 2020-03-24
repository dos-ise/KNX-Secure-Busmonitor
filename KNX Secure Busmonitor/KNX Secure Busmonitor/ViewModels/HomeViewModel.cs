﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Busmonitor.ViewModels
{
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

  using Plugin.Toast;

  using Xamarin.Forms;

  using Device = Xamarin.Forms.Device;

  public class HomeViewModel : INotifyPropertyChanged
  {
    private readonly Settings _settings;
    private Bus _bus;

    private string targetWriteAddress;
    private string writeValue;

    public HomeViewModel(Settings settings)
    {
      _settings = settings;
      _bus = new Bus(CreateParameter());
      Telegramms = new ObservableCollection<Telegramm>();
      ConnectCommand = new Command(OnConnect);
      OpenWriteCommand = new Command(OnOpenWrite);
      WriteCommand = new Command(OnWrite);
      TargetWriteAddress = "1/1/1";
      WriteValue = "true";
    }

    public ICommand ConnectCommand { get; }
    public ICommand OpenWriteCommand { get; }
    public ICommand WriteCommand { get; }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    public string ConnectButtonText => _bus.IsConnected ? "Connect" : "Disconnect";

    public Color ConnectButtonColor => _bus.IsConnected ? Color.GreenYellow : Color.Red;

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

    private void OnConnect()
    {
      ConnectorParameters connectorParameter = CreateParameter();
      if (_bus.IsConnected)
      {
        Task.Run(() => Action(connectorParameter));
      }
      else
      {
        _bus.Disconnect();
        Telegramms.Clear();
        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
      }
    }

    private void Action(ConnectorParameters connectorParameter)
    {
      _bus = new Bus(connectorParameter);

      try
      {
        _bus.Connect();
        OnPropertyChanged(nameof(ConnectButtonColor));
        OnPropertyChanged(nameof(ConnectButtonText));
      }
      catch (Exception exception)
      {
        Device.BeginInvokeOnMainThread(
          () =>
            {
              CrossToastPopUp.Current.ShowToastMessage(
                "Could not connect to " + _settings.InterfaceName + "(" + _settings.IP + ")");
            });

        return;
      }

      if (_bus.IsConnected)
      {
        Device.BeginInvokeOnMainThread(
          () =>
            {
              CrossToastPopUp.Current.ShowToastMessage(
                "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
            });

        var senderAddress = _bus.LocalIndividualAddress;
        _bus.GroupValueReceived += args =>
          {
            Device.BeginInvokeOnMainThread(() =>
              {
                Telegramms.Add(new Telegramm(args, DateTime.Now));
                OnPropertyChanged(nameof(Telegramms));
              });
          };

        while (_bus.IsConnected)
        {
        }
      }
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
