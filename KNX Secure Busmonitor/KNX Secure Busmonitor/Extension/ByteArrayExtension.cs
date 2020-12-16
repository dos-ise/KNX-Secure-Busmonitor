using System;
using System.Collections.Generic;
using System.Text;

namespace Busmonitor.Extension
{
  public static class ByteArrayExtension
  {
    public static string AsHexString(this byte[] ba)
    {
      StringBuilder hex = new StringBuilder(ba.Length * 2);
      foreach (byte b in ba)
        hex.AppendFormat("{0:x2}", b);
      return hex.ToString();
    }
  }
}
