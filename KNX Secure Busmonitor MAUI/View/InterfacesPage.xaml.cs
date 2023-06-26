using KNX_Secure_Busmonitor_MAUI.ViewModel;

namespace KNX_Secure_Busmonitor_MAUI
{
    public partial class InterfacesPage : ContentPage
    {
        public InterfacesPage()
        {
            InitializeComponent();
            BindingContext = new InterfacesViewModel();
        }
    }
}