using System;
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

  using Knx.Bus.Common.Configuration;
  using Knx.Falcon.Sdk;

  using Plugin.Toast;

  using Xamarin.Forms;

  using Device = Xamarin.Forms.Device;

  public class HomeViewModel : INotifyPropertyChanged
  {
    private readonly Settings _settings;

    private string _connectButtonText;
    private Color _connectButtonColor;

    public HomeViewModel(Settings settings)
    {
      _settings = settings;
      _connectButtonText = "Connect";
      _connectButtonColor = Color.GreenYellow;
      Telegramms = new ObservableCollection<Telegramm>();
      ConnectCommand = new Command(OnConnect);
    }

    public ICommand ConnectCommand { get; }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    public string ConnectButtonText
    {
      get
      {
        return _connectButtonText;
      }
      set
      {
        _connectButtonText = value;
        OnPropertyChanged(nameof(ConnectButtonText));
      }
    }    
    
    public Color ConnectButtonColor
    {
      get
      {
        return _connectButtonColor;
      }
      set
      {
        _connectButtonColor = value;
        OnPropertyChanged(nameof(ConnectButtonColor));
      }
    }

    private void OnConnect()
    {
      ConnectorParameters connectorParameter = CreateParameter();
      if (ConnectButtonText != "Disconnect")
      {
        Task.Run(
          () =>
            {
              using (var bus = new Bus(connectorParameter))
              {
                try
                {
                  bus.Connect();
                }
                catch (Exception exception)
                {
                  Device.BeginInvokeOnMainThread(
                    () =>
                      {
                        ConnectButtonText = "Connect";
                        ConnectButtonColor = Color.GreenYellow;
                        CrossToastPopUp.Current.ShowToastMessage(
                          "Could not connect to " + _settings.InterfaceName + "(" + _settings.IP + ")");
                      });

                  return;
                }

                if (bus.IsConnected)
                {
                  Device.BeginInvokeOnMainThread(
                    () =>
                      {
                        CrossToastPopUp.Current.ShowToastMessage(
                          "Connected to " + _settings.InterfaceName + "(" + _settings.IP + ")");
                      });

                  var senderAddress = bus.LocalIndividualAddress;
                  bus.GroupValueReceived += args =>
                    {
                      Device.BeginInvokeOnMainThread(() =>
                        {
                          Telegramms.Add(new Telegramm(args, DateTime.Now));
                          OnPropertyChanged(nameof(Telegramms));
                        });
                    };

                  ////TODO pretty hacky to cancel by text
                  while (ConnectButtonText == "Disconnect")
                  {
                  }
                }
              }
            });
        ConnectButtonText = "Disconnect";
        ConnectButtonColor = Color.Red;
      }
      else
      {
        Telegramms.Clear();
        ConnectButtonText = "Connect";
        ConnectButtonColor = Color.GreenYellow;
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
