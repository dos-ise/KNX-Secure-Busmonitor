using Knx.Bus.Common;

namespace Busmonitor.Model
{
  public class ImportGroupAddress
  {
    private GroupAddress _internalGA;

    public ImportGroupAddress(string addressString)
    {
      _internalGA = new GroupAddress(addressString);
    }

    public string GroupName { get; set; }

    public ushort Address => _internalGA.Address;

    public override string ToString()
    {
      return GroupName + "(" + Address + ")";
    }
  }
}
