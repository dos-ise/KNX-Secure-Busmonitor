using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

using Busmonitor.Model;
using Xamarin.Forms;
using Xamarin.Essentials;
using System;
using System.IO;
using Busmonitor.Bootstrap;

namespace Busmonitor.ViewModels
{
  public class GroupAddressImportViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private readonly INotificationManager _manager;

    public GroupAddressImportViewModel(Settings settings, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      ImportCommand = new Command(OnImport);
      GaCount = _settings.ImportGroupAddress.Count;
    }

    public int GaCount { get; set; }

    private async void OnImport()
    {
      var fileData = await FilePicker.PickAsync();
      if (fileData == null)
        return; // user canceled file picking
      try
      {
        var stream = await fileData.OpenReadAsync();
        StreamReader reader = new StreamReader(stream);
        string contents = reader.ReadToEnd();
        _settings.ImportGroupAddress = GetGa(contents).ToList(); 
        GaCount = _settings.ImportGroupAddress.Count;
        OnPropertyChanged(nameof(GaCount));
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
          var ga = c.Slice(1, 4).Select(a => a.Replace("\"", string.Empty)).Select(Selector);
          i = new ImportGroupAddress()
          {
            GroupName = c[0],
            AddressString = string.Join(" ", ga)
          };
        
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
  }
}
