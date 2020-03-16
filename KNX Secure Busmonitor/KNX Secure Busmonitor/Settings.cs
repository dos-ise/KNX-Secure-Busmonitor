using Xamarin.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Busmonitor
{
  public class Settings : INotifyPropertyChanged
  {
    public string IP
    {
      set => Set(nameof(IP), value);
      get => Get<string>(nameof(IP));
    }    
    
    public ushort IpPort
    {
      set => Set(nameof(IpPort), value);
      get => Get<ushort>(nameof(IpPort));
    }

    public string InterfaceName
    {
      set => Set(nameof(InterfaceName), value);
      get => Get<string>(nameof(InterfaceName));
    }        
    
    public string SerialNumber
    {
      set => Set(nameof(SerialNumber), value);
      get => Get<string>(nameof(SerialNumber));
    }       
    
    public string MediumType
    {
      set => Set(nameof(MediumType), value);
      get => Get<string>(nameof(MediumType));
    }    

    public bool IsSecurityEnabled
    {
      set => Set(nameof(IsSecurityEnabled), value);
      get => Get<bool>(nameof(IsSecurityEnabled));
    }

    public string Password
    {
      set => Set(nameof(Password), value);
      get => Get<string>(nameof(Password));
    }

    public string MacAddress
    {
      set => Set(nameof(MacAddress), value);
      get => Get<string>(nameof(MacAddress));
    }

    private void Set<T>(string key, T value)
    {
      if (Application.Current.Properties.ContainsKey(key))
      {
        Application.Current.Properties[key] = value;
      }
      else
      {
        Application.Current.Properties.Add(key, value);
      }

      Application.Current.SavePropertiesAsync();
      OnPropertyChanged(key);
    }

    private T Get<T>(string key)
    {
      if (Application.Current.Properties.ContainsKey(key))
      {
        return (T)Application.Current.Properties[key];
      }
      else
      {
        return default;
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
