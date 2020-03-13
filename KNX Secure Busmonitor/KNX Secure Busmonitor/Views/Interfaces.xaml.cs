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
  using Plugin.Toast;

  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class Interfaces : ContentPage
  {
    public Interfaces()
    {
      InitializeComponent();
    }
  }
}