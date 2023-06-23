using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    [INotifyPropertyChanged]
    internal partial class MainViewModel
    {
        public MainViewModel()
        {
        }

        [ObservableProperty]
        private List<string> telegrams;        
        
        [ObservableProperty]
        private string selectedTelegram;        
        
        [ObservableProperty]
        private bool isRefreshing;

        [RelayCommand]
        private void Refresh()
        {
            Console.WriteLine("Hello!");
        }
    }
}
