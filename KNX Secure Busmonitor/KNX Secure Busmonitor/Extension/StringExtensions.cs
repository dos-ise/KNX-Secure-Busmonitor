using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace Busmonitor.Extension
{
  public static class StringExtensions
  {
    public static SecureString ToSecureString(this string self)
    {
      SecureString knox = new SecureString();
      char[] chars = self.ToCharArray();
      foreach (char c in chars)
      {
        knox.AppendChar(c);
      }
      return knox;
    }


    public static Stream ToStream(this string s)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      writer.Write(s);
      writer.Flush();
      stream.Position = 0;
      return stream;
    }
  }
}
