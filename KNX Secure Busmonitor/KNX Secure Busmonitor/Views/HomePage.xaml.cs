using Busmonitor.Extension;
using Busmonitor.Model;
using Busmonitor.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor.Views
{
  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class HomePage : ContentPage
  {
    public static Action ScrollToBottom;

    public HomePage()
    {
      InitializeComponent();
      ScrollToBottom = () => 
      {
        var i = TelegrammGrid.ItemsSource as ObservableCollection<Telegramm>;
        TelegrammGrid.ScrollTo(i.Last(), ScrollToPosition.End); 
      };
    }
  }
}