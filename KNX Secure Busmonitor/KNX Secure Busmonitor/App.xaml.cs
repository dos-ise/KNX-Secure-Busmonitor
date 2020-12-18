using Busmonitor.Bootstrap;
using Busmonitor.Views;
using Busmonitor.ViewModels;
using Xamarin.Forms;

namespace Busmonitor
{
  public partial class App : Application
  {
    public static AppShellViewModel Home { get; private set; }

    public App()
    {
      InitializeComponent();

      DependencyService.Get<INotificationManager>().Initialize();

      Xamarin.Forms.DataGrid.DataGridComponent.Init();
      MainPage = new Views.AppShellView();
      Home = MainPage.BindingContext as AppShellViewModel;
    }

    protected override void OnStart()
    {
    }

    protected override void OnSleep()
    {
    }

    protected override void OnResume()
    {
    }
  }
}
