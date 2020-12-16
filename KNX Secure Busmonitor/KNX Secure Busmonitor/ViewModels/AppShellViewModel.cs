namespace Busmonitor.ViewModels
{
  public class AppShellViewModel : ViewModelBase
  {
    public AppShellViewModel(Settings settings)
    {
      Settings = settings;
    }
    
    public Settings Settings { get; }
  }
}
