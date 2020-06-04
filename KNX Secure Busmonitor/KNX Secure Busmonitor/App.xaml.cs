﻿using Busmonitor.Views;

using Xamarin.Forms;

namespace Busmonitor
{
  using Busmonitor.ViewModels;

  public partial class App : Application
  {
    public static NavigationPage NavigationPage { get; private set; }
    public static HomeViewModel Home { get; private set; }
    private static RootPage RootPage;

    public static bool MenuIsPresented
    {
      get => RootPage.IsPresented;
      set => RootPage.IsPresented = value;
    }

    public App()
    {
      InitializeComponent();
      var _settings = new Settings();
      DefaultSettings(_settings);
      Xamarin.Forms.DataGrid.DataGridComponent.Init();
      Home = new HomeViewModel(_settings);
      MainPage = new AppShell() { BindingContext = Home };
    }

    private void DefaultSettings(Settings settings)
    {
      if (string.IsNullOrEmpty(settings.IP))
      {
        settings.IP = "192.168.10.100";
        settings.IpPort = 0x0e57;
        settings.InterfaceName = "Multicast";
        settings.MediumType = "TP";
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
