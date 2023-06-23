using KNX_Secure_Busmonitor_MAUI.ViewModel;

namespace KNX_Secure_Busmonitor_MAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }
    }
}