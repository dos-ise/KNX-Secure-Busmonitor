using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;

using Knx.Bus.Common;
using Knx.Bus.Common.Configuration;
using Knx.Falcon.Sdk;

using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;

using Xamarin.Forms.DataGrid;

using Device = Xamarin.Forms.Device;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor.Views
{
  using Busmonitor.Model;

  using Knx.Bus.Common.KnxIp;

  using Plugin.Toast;

  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class HomePage : ContentPage
  {
    private readonly Settings _settings;

    private string _fileName = string.Empty;

    public HomePage(Settings settings)
    {
      _settings = settings;
      InitializeComponent();

      Telegramms = new ObservableCollection<Telegramm>();
      TelegrammGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding("."));
      TelegrammGrid.BindingContext = Telegramms;
    }

    public ObservableCollection<Telegramm> Telegramms { get; set; }

    void OnConnectButtonClicked(object sender, EventArgs e)
    {
      ConnectorParameters connectorParameter = CreateParameter();
      if (ConnectButton.Text != "Disconnect")
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
                      ConnectButton.Text = "Connect";
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
                while (ConnectButton.Text == "Disconnect")
                {
                }
              }
            }
          });
        ConnectButton.Text = "Disconnect";
        AddKeyringButton.IsEnabled = false;
      }
      else
      {
        Telegramms.Clear();
        ConnectButton.Text = "Connect";
        AddKeyringButton.IsEnabled = true;
      }
    }

    private ConnectorParameters CreateParameter()
    {
      if (string.IsNullOrEmpty(_fileName))
      {
        return new KnxIpTunnelingConnectorParameters(_settings.IP, _settings.IpPort, false);
      }
      else
      {
        //var secure = new KnxIpSecureDeviceManagementConnectorParameters(SelectedInterface);
        //secure.LoadSecurityData(SelectedInterface.IndividualAddress, _fileName, MakeStringSecure(passwordEntry.Text));
        //return secure;
      }

      return null;
    }

    private SecureString MakeStringSecure(string plain)
    {
      //Not very good handling
      SecureString sec = new SecureString();
      string pwd = plain; /* Not Secure! */
      pwd.ToCharArray().ToList().ForEach(sec.AppendChar);
      /* and now : seal the deal */
      sec.MakeReadOnly();
      return sec;
    }

    public async void OnAddButtonClicked(object sender, EventArgs e)
    {
      FileData fileData = await CrossFilePicker.Current.PickFile();
      if (fileData == null)
        return; // user canceled file picking
      _fileName = fileData.FileName;
    }

    public void OnSaveButtonClicked(object sender, EventArgs e)
    {
      string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Telegramms.csv");
      //TODO add content
      var csvContent = string.Empty;
      File.WriteAllText(fileName, csvContent);
    }

    //private static string ConvertToCsv(Telegram telegram, int counter)
    //{
    //  return String.Join(",",
    //    counter,
    //    $"\"{DateTimeFormatter.ConvertToDateTime(telegram.TimestampLocal, DateAndTime)}\"",
    //    telegram.Service,
    //    telegram.Flags,
    //    telegram.Priority,
    //    telegram.SourceAddress,
    //    telegram.SourceName,
    //    telegram.Destination,
    //    telegram.DestinationName,
    //    telegram.RoutingCounter,
    //    telegram.Type,
    //    telegram.DataPointType,
    //    $"\"{telegram.Info}\"",
    //    telegram.Iack);
    //}
  }
}