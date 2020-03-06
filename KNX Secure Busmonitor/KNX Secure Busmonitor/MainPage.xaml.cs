using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Security;

using Knx.Bus.Common.Configuration;
using Knx.Bus.Common.KnxIp;
using Knx.Falcon.Sdk;

using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace KNX_Secure_Busmonitor
{
  using System.Collections.ObjectModel;

  using Device = Xamarin.Forms.Device;

  // Learn more about making custom code visible in the Xamarin.Forms previewer
  // by visiting https://aka.ms/xamarinforms-previewer
  [DesignTimeVisible(false)]
  public partial class MainPage : ContentPage
  {
    private string _fileName = string.Empty;

    public MainPage()
    {
      InitializeComponent();

      Task.Run(() =>
        {
          foreach (var dis in Discover())
          {
            DiscoveredInterfaces.Add(dis);
          }
        });
      DiscoveredInterfaces = new ObservableCollection<DiscoveryResult>();
      listView.SetBinding(ListView.ItemsSourceProperty, new Binding("."));
      listView.BindingContext = DiscoveredInterfaces;
      ToggleUI();
    }

    private void ToggleUI()
    {
      ////TODO replace with binding
      if (SelectedInterface == null)
      {
        listView.IsVisible = true;
        ButtonGrid.IsVisible = false;
        editor.IsVisible = false;
        passwordLabel.IsVisible = false;
        passwordEntry.IsVisible = false;
      }
      else
      {
        listView.IsVisible = false;
        ButtonGrid.IsVisible = true;
        editor.IsVisible = true;
        passwordLabel.IsVisible = true;
        passwordEntry.IsVisible = true;
      }
    }

    private IEnumerable<DiscoveryResult> Discover()
    {
      return new DiscoveryClient(AdapterTypes.All).Discover();
    }

    public ObservableCollection<DiscoveryResult> DiscoveredInterfaces { get; set; }

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
                if (bus.IsConnected)
                {
                  Log("Connected to " + bus.OpenParameters);
                }
                else
                {
                  return;
                }

                var senderAddress = bus.LocalIndividualAddress;
                Log("LocalIndividualAddress: " + senderAddress);
                bus.GroupValueReceived += args =>
                  {
                    Log(
                      "IndividualAddress: " + args.IndividualAddress + " Value: " + args.Value + " Address:"
                      + args.Address);
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
        editor.Text = string.Empty;
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

    private void Log(string message)
    {
      Device.BeginInvokeOnMainThread(() => { editor.Text += message + Environment.NewLine; });
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
  }
}
