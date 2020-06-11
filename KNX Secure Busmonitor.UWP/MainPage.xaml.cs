namespace KNX_Secure_Busmonitor.UWP
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage   
  {
    public MainPage()
    {
      InitializeComponent();
      LoadApplication(new Busmonitor.App());
    }
  }
}
