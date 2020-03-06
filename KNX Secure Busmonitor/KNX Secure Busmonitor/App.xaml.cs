namespace Busmonitor
{
  using Xamarin.Forms;

  public partial class App : Application
  {
    public App()
    {
      InitializeComponent();
      Xamarin.Forms.DataGrid.DataGridComponent.Init();
      MainPage = new MainPage();
      //MainPage = new MainPage() { BindingContext = new ViewModels.MainViewModel() }; 
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
