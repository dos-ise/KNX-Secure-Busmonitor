using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Knx.Falcon;
using KNX_Secure_Busmonitor_MAUI.Model;

namespace KNX_Secure_Busmonitor_MAUI.ViewModel
{
    [INotifyPropertyChanged]
    internal partial class GroupAddressImportViewModel
    {
        public GroupAddressImportViewModel()
        {

        }

        [RelayCommand]
        private async void Import()
        {
            try
            {
                var stream = await PickFile();
                StreamReader reader = new StreamReader(stream);
                string contents = reader.ReadToEnd();
                var gas = GetGa(contents).ToList();

             }
            catch (Exception ex)
            {
                ////TODO
            }
        }

        private async Task<Stream> PickFile()
        {
            var fileData = await FilePicker.PickAsync();
            if (fileData == null)
            {
                return null;
            }

            return await fileData.OpenReadAsync();
        }

        private IEnumerable<ImportGroupAddress> GetGa(string gaExport)
        {
            var lines = gaExport.GetLines().ToList();
            lines.RemoveAt(0);
            lines.RemoveAt(lines.Count - 1);
            foreach (var line in lines)
            {
                ImportGroupAddress i = null;
                try
                {
                    var c = line.GetColumns().ToArray();
                    var slices = c.Slice(1, 2);
                    var ga = slices.Select(a => a.Replace("\"", string.Empty)).Select(Selector);
                    var addressString = string.Join(" ", ga);
                    if (GroupAddress.TryParse(addressString, out var gaa))
                    {
                        i = new ImportGroupAddress()
                        {
                            GroupName = c[0],
                            AddressString = addressString
                        };
                    }
                }
                catch (Exception e)
                {
                }

                if (i != null)
                {
                    yield return i;
                }
            }
        }

        private string Selector(string arg)
        {
            return string.IsNullOrEmpty(arg) ? "0" : arg;
        }
    }
}
