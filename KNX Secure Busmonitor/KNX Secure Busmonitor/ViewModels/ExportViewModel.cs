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
      var communiLog = new XElement(nameSpace + "CommunicationLog");
      var timeStamp = new XAttribute("Timestamp", "2020-03-13T14:40:19.0278597Z");
      var connection = new XElement(nameSpace + "Connection", timeStamp, new XAttribute("State", "Established"));
      var recordStart = new XElement(nameSpace + "RecordStart", timeStamp);
      var recordStop = new XElement(nameSpace + "RecordStop", timeStamp);

      communiLog.Add(connection);
      communiLog.Add(recordStart);
      foreach (var tele in telegrams)
      {
        communiLog.Add(CreateTeleXml(nameSpace, tele));
      }
      communiLog.Add(recordStop);

      var file = new XDocument(communiLog);

      return file;
    }

    private XElement CreateTeleXml(XNamespace nameSpace, GroupValueEventArgs tele)
    {
      var rawData = new XAttribute("RawData", ConvertToRaw(tele));
      //var timeStamp = new XAttribute("Timestamp", );
      var telegram = new XElement(nameSpace + "Telegram", rawData);
      return telegram;
    }

    private string ConvertToRaw(GroupValueEventArgs tele)
    {
      //TODO
      return "2B0703010604020703BC30045425E1008166";
    }
  }
}
