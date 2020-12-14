using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

using Busmonitor.Model;
using Xamarin.Forms;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System;
using Plugin.LocalNotifications;

namespace Busmonitor.ViewModels
{
  public class GroupAddressImportViewModel : ViewModelBase
  {
    private readonly Settings _settings;

    public GroupAddressImportViewModel(Settings settings)
    {
      _settings = settings;
      ImportCommand = new Command(OnImport);
      //GaCount = _settings.ImportGroupAddress.Count;
      //OnPropertyChanged(nameof(GaCount));
    }

    public int GaCount { get; set; }

    private async void OnImport()
    {
      FileData fileData = await CrossFilePicker.Current.PickFile();
      if (fileData == null)
        return; // user canceled file picking
      try
      {
        string contents = System.Text.Encoding.UTF8.GetString(fileData.DataArray);
        _settings.ImportGroupAddress = GetGa(contents).ToList(); 
        GaCount = _settings.ImportGroupAddress.Count;
        OnPropertyChanged(nameof(GaCount));
      }
      catch (Exception ex)
      {
        Device.BeginInvokeOnMainThread(
            () =>
            {
              CrossLocalNotifications.Current.Show("Error:", "Could not import GA (" + ex.Message + ")");
            });
      }
    }

    private IEnumerable<ImportGroupAddress> GetGa(string gaExport)
    {
      var lines = gaExport.GetLines().ToList();
      lines.RemoveAt(0);
      lines.RemoveAt(lines.Count - 1);
      foreach (var line in lines)
      {
        var c = line.GetColumns().ToArray();
        var ga = c.Slice(1, 4).Select(a => a.Replace("\"", string.Empty)).Select(Selector);
        var i = new ImportGroupAddress(string.Join(" ", ga))
        {
          GroupName = c[0]
        };
        yield return i;
      }
    }

    private string Selector(string arg)
    {
      return string.IsNullOrEmpty(arg) ? "0" : arg;
    }

    public ICommand ImportCommand { get; }
  }
}
