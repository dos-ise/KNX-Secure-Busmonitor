using System.Windows.Input;

namespace Busmonitor.ViewModels
{
  using System.Collections.Generic;
  using System.IO;
  using System.Xml.Linq;

  using Busmonitor.Model;

  using Knx.Bus.Common;

  using Xamarin.Essentials;
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

    private async void ExecuteExport(object obj)
    {
      XDocument exportFile = CreateExportFile(new List<Telegramm>());

      var fn = "Telegrams.xml";
      var file = Path.Combine(FileSystem.CacheDirectory, fn);
      exportFile.Save(file);

      await Share.RequestAsync(new ShareFileRequest
                                 {
                                   Title = "Telegrams",
                                   File = new ShareFile(file)
                                 });
    }

    private XDocument CreateExportFile(IEnumerable<Telegramm> telegrams)
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

    private XElement CreateTeleXml(XNamespace nameSpace, Telegramm tele)
    {
      var rawData = new XAttribute("RawData", tele.RAW);
      var timeStamp = new XAttribute("Timestamp", tele.TimeStamp);
      var telegram = new XElement(nameSpace + "Telegram", rawData);
      return telegram;
    }
  }
}
