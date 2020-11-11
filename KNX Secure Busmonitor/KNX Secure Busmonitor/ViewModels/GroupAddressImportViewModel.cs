using System.Windows.Input;

using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Text.RegularExpressions;

  using Busmonitor.Model;

  public class GroupAddressImportViewModel
  {
    public GroupAddressImportViewModel()
    {
      ImportCommand = new Command(OnImport);
      ImportedGroupAddresses = new ObservableCollection<ImportGroupAddress>();
    }

    private void OnImport()
    {
      var lines = Import.GetLines().ToList();
      lines.RemoveAt(0);
      lines.RemoveAt(lines.Count -1);
      foreach (var line in lines)
      {
        var c = line.GetColumns().ToArray();
        var ga = c.Slice(1, 4).Select(a => a.Replace("\"", string.Empty)).Select(Selector);
        var i = new ImportGroupAddress(string.Join(" ", ga))
          {
            GroupName = c[0]
          };
        ImportedGroupAddresses.Add(i);
      }
    }

    private string Selector(string arg)
    {
      return string.IsNullOrEmpty(arg) ? "0" : arg;
    }

    public ICommand ImportCommand { get; }

    public string Import { get; set; }

    public ObservableCollection<ImportGroupAddress> ImportedGroupAddresses { get; }
  }
}
