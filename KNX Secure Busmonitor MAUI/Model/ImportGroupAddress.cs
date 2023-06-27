
using Knx.Falcon;

namespace KNX_Secure_Busmonitor_MAUI.Model
{
  public class ImportGroupAddress
  {
    private GroupAddress _internalGA;
    private string _addressString;
    
    public string AddressString
    {
      get => _addressString;
      set
      {
        _internalGA = new GroupAddress(value);
        _addressString = value;
      }
    }

    public string GroupName { get; set; }

    public ushort Address => _internalGA.Address;

    public override string ToString()
    {
      return GroupName + "(" + Address + ")";
    }
  }
}
