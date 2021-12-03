using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

using Busmonitor.Model;
using Xamarin.Forms;
using Xamarin.Essentials;
using System;
using System.IO;
using System.Threading.Tasks;
using Busmonitor.Bootstrap;
using Knx.Falcon;

namespace Busmonitor.ViewModels
{
  public class GroupAddressImportViewModel : ViewModelBase
  {
    private readonly ISettings _settings;
    private readonly INotificationManager _manager;
    public Func<Task<Stream>> _pickAsync = null;

    public GroupAddressImportViewModel(ISettings settings, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      _pickAsync = PickFile;
      ImportCommand = new Command(OnImport);
      ItemSelectedCommand = new Command(ItemSelectedExecute);
    }

    private async void ItemSelectedExecute(object obj)
    {
      var args = obj as SelectedItemChangedEventArgs;
      if (args?.SelectedItem is ImportGroupAddress result)
      {
        await Clipboard.SetTextAsync(result.AddressString);
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

    public int GaCount => _settings.ImportGroupAddress.Count;

    public List<ImportGroupAddress> ImportGroupAddress => _settings.ImportGroupAddress;

    private async void OnImport()
    {
      try
      {
        var stream = await _pickAsync();
        StreamReader reader = new StreamReader(stream);
        string contents = reader.ReadToEnd();
        var gas= GetGa(contents).ToList();
        _settings.ImportGroupAddress = gas;
        OnPropertyChanged(nameof(GaCount));
        OnPropertyChanged(nameof(ImportGroupAddress));
      }
      catch (Exception ex)
      {
        Device.BeginInvokeOnMainThread(
            () =>
            {
              _manager.SendNotification("Error:", "Could not import GA (" + ex.Message + ")");
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

    public ICommand ImportCommand { get; }
    
    public ICommand ItemSelectedCommand { get; }
  }
}
