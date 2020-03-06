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
    }

    void OnConnectButtonClicked(object sender, EventArgs e)
    {
      var discoveryResults = new DiscoveryClient(AdapterTypes.All).Discover().ToList();
      foreach (var result in discoveryResults)
      {
        Log((discoveryResults.IndexOf(result)) + ": " + result.FriendlyName + " (" + result.IpAddress + ")");
      }

      if (!discoveryResults.Any()) return;

      ConnectorParameters selectedConnector = SelectConnector(discoveryResults);
      Task.Run(
        () =>
          {
            using (var bus = new Bus(selectedConnector))
            {
              bus.Connect();
              if (bus.IsConnected)
              {
                Log("Connected to " + bus.OpenParameters);
              }

              var senderAddress = bus.LocalIndividualAddress;
              Log("LocalIndividualAddress: " + senderAddress);
              bus.GroupValueReceived += args =>
                {
                  Log(
                    "IndividualAddress: " + args.IndividualAddress + " Value: " + args.Value + " Address:"
                    + args.Address);
                };

              //TODO cancel Task
              while (true) { }
            }
          });
    }

    private ConnectorParameters SelectConnector(List<DiscoveryResult> discoveryResults)
    {
      ////TODO add UI to select Connector
      var selected = discoveryResults.FirstOrDefault();
      if (string.IsNullOrEmpty(_fileName))
      {
        return new KnxIpTunnelingConnectorParameters(selected);
      }
      else
      {
        var secure = new KnxIpSecureDeviceManagementConnectorParameters(selected);
        secure.LoadSecurityData(selected.IndividualAddress, _fileName, MakeStringSecure(password.Text));
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

    public void OnClearButtonClicked(object sender, EventArgs e)
    {
      editor.Text = string.Empty;
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
  }
}
