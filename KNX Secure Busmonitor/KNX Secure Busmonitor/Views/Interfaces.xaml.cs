using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Knx.Bus.Common.KnxIp;
using Knx.Falcon.Sdk;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor.Views
{
  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class Interfaces : ContentPage
  {
    public Interfaces()
    {
      InitializeComponent();
    }


    private void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
      SelectedInterface = e.SelectedItem as DiscoveryResult;
    }

    public DiscoveryResult SelectedInterface { get; set; }

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

    private IEnumerable<DiscoveryResult> Discover()
    {
      return new DiscoveryClient(AdapterTypes.All).Discover();
    }


    private void DiscoverButton_OnClicked(object sender, EventArgs e)
    {
      DiscoveredInterfaces.Clear();
      SelectedInterface = null;
      listView.SelectedItem = null;
      DiscoverInterfaces();
    }

    public ObservableCollection<DiscoveryResult> DiscoveredInterfaces { get; set; }
  }
}