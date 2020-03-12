namespace Busmonitor
{
  using Busmonitor.Views;

  using Xamarin.Forms;

  public partial class App : Application
  {
    public static NavigationPage NavigationPage { get; private set; }
    private static RootPage RootPage;

    public static bool MenuIsPresented
    {
      get
      {
        return RootPage.IsPresented;
      }
      set
      {
        RootPage.IsPresented = value;
      }
    }

    public App()
    {
      InitializeComponent();
      Xamarin.Forms.DataGrid.DataGridComponent.Init();
      var menuPage = new MenuPage();
      menuPage.Title = "Menu";
      NavigationPage = new NavigationPage(new HomePage());
      RootPage = new RootPage();
      RootPage.Master = menuPage;
      RootPage.Detail = NavigationPage;
      MainPage = RootPage;
      //MainPage = new MainPage();
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
