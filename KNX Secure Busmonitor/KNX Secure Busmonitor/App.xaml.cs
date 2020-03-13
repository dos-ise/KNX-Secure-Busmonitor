﻿namespace Busmonitor
{
  using Busmonitor.Views;

  using Knx.Bus.Common.KnxIp;
  using Knx.Falcon.Sdk;

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
      var _settings = new Settings();
      DefaultSettings(_settings);
      Xamarin.Forms.DataGrid.DataGridComponent.Init();
      var menuPage = new MenuPage();
      menuPage.Title = "Menu";
      NavigationPage = new NavigationPage(new HomePage(_settings));
      RootPage = new RootPage();
      RootPage.Master = menuPage;
      RootPage.Detail = NavigationPage;
      MainPage = RootPage;
      //MainPage = new MainPage();
      //MainPage = new MainPage() { BindingContext = new ViewModels.MainViewModel() }; 
    }

    private void DefaultSettings(Settings settings)
    {
      if (string.IsNullOrEmpty(settings.IP))
      {
        settings.IP = "224.0.23.12";
        settings.IpPort = 0x0e57;
        settings.InterfaceName = "Multicast";
      }
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
