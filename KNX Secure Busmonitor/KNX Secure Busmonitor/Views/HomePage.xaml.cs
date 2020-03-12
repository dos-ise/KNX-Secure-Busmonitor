using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;

using Knx.Bus.Common;
using Knx.Bus.Common.Configuration;
using Knx.Bus.Common.KnxIp;
using Knx.Falcon.Sdk;

using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;

using Xamarin.Forms.DataGrid;

using Device = Xamarin.Forms.Device;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor.Views
{
  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class HomePage : ContentPage
  {
    private string _fileName = string.Empty;

    public HomePage()
    {
      InitializeComponent();


      DiscoveredInterfaces = new ObservableCollection<DiscoveryResult>();
      DiscoverInterfaces();
      Telegramms = new ObservableCollection<GroupValueEventArgs>();

      listView.SetBinding(ListView.ItemsSourceProperty, new Binding("."));
      listView.BindingContext = DiscoveredInterfaces;

      TelegrammGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding("."));
      TelegrammGrid.BindingContext = Telegramms;
      ToggleUI();
    }

    private void DiscoverInterfaces()
    {
      Task.Run(() =>
      {
        foreach (var dis in Discover())
        {
          DiscoveredInterfaces.Add(dis);
        }
      });
    }

    private void ToggleUI()
    {
      ////TODO replace with binding
      if (SelectedInterface == null)
      {
        listGrid.IsVisible = true;
        ButtonGrid.IsVisible = false;
        TelegrammGrid.IsVisible = false;
        //passwordLabel.IsVisible = false;
        //passwordEntry.IsVisible = false;
      }
      else
      {
        listGrid.IsVisible = false;
        ButtonGrid.IsVisible = true;
        TelegrammGrid.IsVisible = true;
        //passwordLabel.IsVisible = true;
        //passwordEntry.IsVisible = true;
      }
    }

    private IEnumerable<DiscoveryResult> Discover()
    {
      return new DiscoveryClient(AdapterTypes.All).Discover();
    }

    public ObservableCollection<DiscoveryResult> DiscoveredInterfaces { get; set; }
    public ObservableCollection<GroupValueEventArgs> Telegramms { get; set; }

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
              bus.Connect();
              if (!bus.IsConnected)
              {
                return;
              }

              var senderAddress = bus.LocalIndividualAddress;
              bus.GroupValueReceived += args =>
              {
                Device.BeginInvokeOnMainThread(() =>
                {
                  Telegramms.Add(args);
                  OnPropertyChanged(nameof(Telegramms));
                });
              };

              //TODO pretty hacky to cancel by text
              while (ConnectButton.Text == "Disconnect")
              {
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
        return new KnxIpTunnelingConnectorParameters(SelectedInterface);
      }
      else
      {
        var secure = new KnxIpSecureDeviceManagementConnectorParameters(SelectedInterface);
        secure.LoadSecurityData(SelectedInterface.IndividualAddress, _fileName, MakeStringSecure(passwordEntry.Text));
        return secure;
      }
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

    private void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
      SelectedInterface = e.SelectedItem as DiscoveryResult;
      ToggleUI();
    }

    public DiscoveryResult SelectedInterface { get; set; }

    public void OnSaveButtonClicked(object sender, EventArgs e)
    {
      string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Telegramms.csv");
      //TODO add content
      var csvContent = string.Empty;
      File.WriteAllText(fileName, csvContent);
    }

    private void DiscoverButton_OnClicked(object sender, EventArgs e)
    {
      DiscoveredInterfaces.Clear();
      Telegramms.Clear();
      SelectedInterface = null;
      listView.SelectedItem = null;
      ToggleUI();
      DiscoverInterfaces();
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