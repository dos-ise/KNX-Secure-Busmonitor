using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Busmonitor.Model;

using Xamarin.Essentials;
using Xamarin.Forms;

namespace Busmonitor.ViewModels
{
  public class ExportViewModel : ViewModelBase
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
      XDocument exportFile = CreateExportFile(App.Home.Telegramms);

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

      XAttribute startTimeStamp;
      XAttribute stopTimeStamp;
      if (telegrams.Any())
      {
        startTimeStamp = new XAttribute("Timestamp", telegrams.First().TimeStamp);
        stopTimeStamp = new XAttribute("Timestamp", telegrams.Last().TimeStamp);
      }
      else
      {
        startTimeStamp = new XAttribute("Timestamp", DateTime.Now);
        stopTimeStamp = new XAttribute("Timestamp", DateTime.Now);
      }
      
      var connection = new XElement(nameSpace + "Connection", startTimeStamp, new XAttribute("State", "Established"));
      var recordStart = CreateRecordStart(nameSpace, startTimeStamp);
      var recordStop = new XElement(nameSpace + "RecordStop", stopTimeStamp);

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

    private object CreateRecordStart(XNamespace nameSpace, XAttribute timeStamp)
    {
      var mode = new XAttribute("Mode", "LinkLayer");
      var host = new XAttribute("Host", "Android");
      var connectionName = new XAttribute("ConnectionName", _settings.InterfaceName);
      var options = string.Format(
        "Type=KnxIpTunneling;HostAddress={0};Name=&quot;{1}&quot;",
        _settings.IP,
        _settings.InterfaceName);
      var connectionOptions = new XAttribute("ConnectionOptions", options);
      var connectorType = new XAttribute("ConnectorType", "KnxIpTunneling");
      var mediumType = new XAttribute("MediumType", _settings.MediumType);
      var record = new XElement(nameSpace + "RecordStart", timeStamp, mode, host, connectionName, connectionOptions, connectorType, mediumType);
      return record;
    }

    private XElement CreateTeleXml(XNamespace nameSpace, Telegramm tele)
    {
      var timeStamp = new XAttribute("Timestamp", tele.TimeStamp);
      var service = new XAttribute("Service", "L_Data.ind");
      var frameFormat = new XAttribute("FrameFormat", "CommonEmi");
      var rawData = new XAttribute("RawData", tele.RAW);
      var telegram = new XElement(nameSpace + "Telegram", timeStamp, service, frameFormat, rawData);
      return telegram;
    }
  }
}
