using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor.Views
{
  [XamlCompilation(XamlCompilationOptions.Compile)]
  public partial class AppShellView : Shell
  {
    public AppShellView()
    {
      InitializeComponent();
    }
  }
}