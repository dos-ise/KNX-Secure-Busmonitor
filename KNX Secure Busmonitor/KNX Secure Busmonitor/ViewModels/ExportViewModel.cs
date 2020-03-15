using System.Windows.Input;

namespace Busmonitor.ViewModels
{
  using System.Collections.Generic;
  using System.Xml.Linq;

  using Knx.Bus.Common;

  using Xamarin.Forms;

  public class ExportViewModel
  {
    private readonly Settings _settings;

    public ICommand ExportCommand { get; }

    public ExportViewModel(Settings settings)
    {
      _settings = settings;
      ExportCommand = new Command(ExecuteExport);
    }

    private void ExecuteExport(object obj)
    {
      XDocument exportFile = CreateExportFile(new List<GroupValueEventArgs>());
    }

    private XDocument CreateExportFile(IEnumerable<GroupValueEventArgs> telegrams)
    {
      XNamespace nameSpace = "http://knx.org/xml/telegrams/01";
      var timeStamp = new XAttribute("Timestamp", "2020-03-13T14:40:19.0278597Z");
      var connection = new XElement("Connection", timeStamp, new XAttribute("State", "Established"));
      var recordStart = new XElement("RecordStart", timeStamp);
      var recordStop = new XElement("RecordStop", timeStamp);
      var file = new XDocument(new XElement(nameSpace + "CommunicationLog"));

      return file;
    }
  }
}
