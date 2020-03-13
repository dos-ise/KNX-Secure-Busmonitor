using System.Windows.Input;
using Busmonitor.Views;

using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  public class MenuViewModel
  {
    public Settings Settings { get; }

    public ICommand GoMonitorCommand { get; set; }
    public ICommand GoInterfacesCommand { get; set; }
    public ICommand GoSecurityCommand { get; set; }
    public ICommand GoExportCommand { get; set; }

    public MenuViewModel(Settings settings)
    {
      Settings = settings;
      GoMonitorCommand = new Command(GoMonitorExecute);
      GoInterfacesCommand = new Command(GoInterfacesExecute);
      GoSecurityCommand = new Command(GoSecurityExecute);
      GoExportCommand = new Command(GoExportExecute);
    }

    private void GoExportExecute(object obj)
    {
      App.MenuIsPresented = false;
    }

    private void GoSecurityExecute(object obj)
    {
      App.MenuIsPresented = false;
    }

    private void GoInterfacesExecute(object obj)
    {
      App.NavigationPage.Navigation.PushAsync(new Interfaces(Settings));
      App.MenuIsPresented = false;
    }

    private void GoMonitorExecute(object obj)
    {
      App.NavigationPage.Navigation.PopToRootAsync();
      App.MenuIsPresented = false;
    }
  }
}
