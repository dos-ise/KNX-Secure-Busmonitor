using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Input;
using Busmonitor.Bootstrap;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  public class SecurtiyViewModel : ViewModelBase
  {
    private readonly Settings _settings;
    private readonly INotificationManager _manager;

    public SecurtiyViewModel(Settings settings, INotificationManager manager)
    {
      _settings = settings;
      _manager = manager;
      ImportKnxKeys = new Command(OnImportKeys);
    }

    private async void OnImportKeys()
    {
      var fileData = await FilePicker.PickAsync();
      if (fileData == null)
        return; // user canceled file picking
      try
      {
        var stream = await fileData.OpenReadAsync();
        StreamReader reader = new StreamReader(stream);
        _settings.KnxKeys = reader.ReadToEnd();
      }
      catch (Exception ex)
      {
        Device.BeginInvokeOnMainThread(
          () =>
          {
            _manager.SendNotification("Error:", "Could not import KNX Keys (" + ex.Message + ")");
          });
      }
    }
    
    public ICommand ImportKnxKeys { get; }
    
    public string Password
    {
      get => _settings.Password;
      set => _settings.Password = value;
    }
  }
}
