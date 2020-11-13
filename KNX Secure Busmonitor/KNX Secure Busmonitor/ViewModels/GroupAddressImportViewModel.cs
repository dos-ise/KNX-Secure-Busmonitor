using System.Windows.Input;

using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  using System.Collections.Generic;
  using System.Linq;

  using Busmonitor.Model;

  public class GroupAddressImportViewModel
  {
    private readonly Settings _settings;

    private string gaExport = "\"Group name\"\t\"Main\"\t\"Middle\"\t\"Sub\"\t\"Central\"\t\"Unfiltered\"\t\"Description\"\t\"DatapointType\"\t\"Security\"\r\n\"Hauptgruppe\"\t\"1\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"Off\"\r\n\"Schalten\"\t\"1\"\t\"1\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"Off\"\r\n\"Homematic Wandtaster\"\t\"1\"\t\"1\"\t\"1\"\t\"\"\t\"\"\t\"Wandtaster im Flur vor dem Wohnzimmer\"\t\"DPST-1-1\"\t\"Off\"\r\n\"Homematic Handsender Kanal 2\"\t\"1\"\t\"1\"\t\"2\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-1\"\t\"Off\"\r\n\"Garagentor\"\t\"1\"\t\"1\"\t\"3\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-19\"\t\"Off\"\r\n\"Tür Terrasse\"\t\"1\"\t\"1\"\t\"4\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-19\"\t\"Off\"\r\n\"Dimmer Kronleuchter Wohnzimmer\"\t\"1\"\t\"1\"\t\"5\"\t\"\"\t\"\"\t\"\"\t\"DPST-5-1\"\t\"Off\"\r\n\"Heizen\"\t\"1\"\t\"2\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"Off\"\r\n\"Aktuelle Temperatur Flur\"\t\"1\"\t\"2\"\t\"1\"\t\"\"\t\"\"\t\"Gemessene Temperatur vom Homematic Wandthermostat\"\t\"DPST-9-1\"\t\"Off\"\r\n\"Aktuelle Luftfeuchtigkeit Flur\"\t\"1\"\t\"2\"\t\"2\"\t\"\"\t\"\"\t\"Relative humidity\"\t\"DPST-5-1\"\t\"Off\"\r\n\"Jalousie Somfy\"\t\"1\"\t\"3\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"Off\"\r\n\"Jalousie Esszimmer\"\t\"1\"\t\"3\"\t\"1\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Wohnzimmer\"\t\"1\"\t\"3\"\t\"2\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Tür Wohnzimmer\"\t\"1\"\t\"3\"\t\"3\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Küche\"\t\"1\"\t\"3\"\t\"4\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Tür Küche\"\t\"1\"\t\"3\"\t\"5\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Mattis\"\t\"1\"\t\"3\"\t\"6\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Emma\"\t\"1\"\t\"3\"\t\"7\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Badezimmer\"\t\"1\"\t\"3\"\t\"8\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Jalousie Schlafzimmer\"\t\"1\"\t\"3\"\t\"9\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n\"Markise Terrasse\"\t\"1\"\t\"3\"\t\"10\"\t\"\"\t\"\"\t\"\"\t\"DPST-1-8\"\t\"Off\"\r\n";

    public GroupAddressImportViewModel(Settings settings)
    {
      _settings = settings;
      ImportCommand = new Command(OnImport);
      //TODO use real data
      Import = gaExport;
    }

    private void OnImport()
    {
      _settings.ImportGroupAddress = GetGa().ToList();
    }

    private IEnumerable<ImportGroupAddress> GetGa()
    {
      var lines = Import.GetLines().ToList();
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

    public string Import { get; set; }
  }
}
