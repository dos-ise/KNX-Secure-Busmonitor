using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Busmonitor;
using Busmonitor.Bootstrap;
using Busmonitor.Model;
using Busmonitor.ViewModels;
using Microsoft.VisualBasic;
using Moq;
using NUnit.Framework;
using Xamarin.Essentials;

namespace KNX_Secure_Busmonitor_Test
{
  [TestFixture]
  public class GroupAddressImportViewModelTest
  {

    [Test]
    public void When_import_should_parse_correct()
    {
      var file = TestFileHelper.GetTestFileFromTemp("20210206_GA.csv");
      var settingsMock = CreateSettingsMock();
      var sut = new GroupAddressImportViewModel(settingsMock.Object, Mock.Of<INotificationManager>())
      {
        _pickAsync = Task.FromResult(File.OpenRead(file) as Stream)
      };

      sut.ImportCommand.Execute(null);

      settingsMock.Verify(me => me.ImportGroupAddress, Times.Once);
      Assert.AreEqual(1815, sut.GaCount);
    }

    private Mock<ISettings> CreateSettingsMock()
    {
      var mock = new Mock<ISettings>();
      mock.Setup(me => me.ImportGroupAddress).Returns(new List<ImportGroupAddress>());
      return mock;
    }
  }
}
