using System;
using Xamarin.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using Busmonitor.Model;
using Knx.Bus.Common;
using Newtonsoft.Json;

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
    
    public string KnxKeys
    {
      set => Set(nameof(KnxKeys), value);
      get => Get<string>(nameof(KnxKeys));
    }
    
    public string MacAddress
    {
      set => Set(nameof(MacAddress), value);
      get => Get<string>(nameof(MacAddress));
    }

    /// <summary>
    /// TODO persist
    /// </summary>
    public List<ImportGroupAddress> ImportGroupAddress
    {
      set => Set(nameof(ImportGroupAddress), value);
      get => Get<List<ImportGroupAddress>>(nameof(ImportGroupAddress));
    }

    public string IndividualAddress
    {
      set => Set(nameof(IndividualAddress), value);
      get => Get<string>(nameof(IndividualAddress));
    }

    private void Set<T>(string key, T value)
    {
      if (value.GetType().IsPrimitive || typeof(string).IsAssignableFrom(typeof(T)))
      {
        if (Application.Current.Properties.ContainsKey(key))
        {
          Application.Current.Properties[key] = value;
        }
        else
        {
          Application.Current.Properties.Add(key, value);
        }
      }
      else
      {
        var valueString = JsonConvert.SerializeObject(value);
        if (Application.Current.Properties.ContainsKey(key))
        {
          Application.Current.Properties[key] = valueString;
        }
        else
        {
          Application.Current.Properties.Add(key, valueString);
        }
      }

      Application.Current.SavePropertiesAsync();
      OnPropertyChanged(key);
    }

    private T Get<T>(string key)
    {
      if (typeof(T).IsPrimitive || typeof(string).IsAssignableFrom(typeof(T)))
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
      else
      {
        if (Application.Current.Properties.ContainsKey(key))
        {

          return JsonConvert.DeserializeObject<T>(Application.Current.Properties[key].ToString());
        }
        else
        {
          return Activator.CreateInstance<T>();
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
