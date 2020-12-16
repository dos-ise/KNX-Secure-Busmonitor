using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Busmonitor
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