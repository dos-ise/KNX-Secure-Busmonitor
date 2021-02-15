using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KNX_Secure_Busmonitor_Test
{
  public static class TestFileHelper
  {
    public static string GetTestFileFromTemp(string fileName)
    {
      var assembly = Assembly.GetExecutingAssembly();
      var tempFile = Path.GetTempFileName().Replace(".tmp", Path.GetExtension(fileName));
      fileName = assembly.GetManifestResourceNames().Where(me => me.Contains(fileName)).Single();
      using (var stream = assembly.GetManifestResourceStream(fileName))
      {
        using (var sr = new FileStream(tempFile, FileMode.OpenOrCreate))
        {
          stream.CopyTo(sr);
        }
      }

      return tempFile;
    }
  }
}
