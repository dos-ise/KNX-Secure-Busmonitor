using KNX_Secure_Busmonitor_MAUI.ViewModel;

namespace KNX_Secure_Busmonitor_MAUI
{
    public partial class GroupAddressImportPage : ContentPage
    {
        public GroupAddressImportPage()
        {
            InitializeComponent();
            BindingContext = new GroupAddressImportViewModel();
        }
    }
}