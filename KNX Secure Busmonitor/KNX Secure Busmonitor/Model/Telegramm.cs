using System;
using System.Collections.Generic;
using System.Text;

namespace Busmonitor.Model
{
  using Knx.Bus.Common;

  public class Telegramm
  {
    public GroupValueEventArgs Args { get; }

    public DateTime TimeStamp { get; }

    public string  RAW => GetRaw();

    private string GetRaw()
    {
      //TODO
      return "2B0703010604020703BC30045425E1008166";
    }

    public Telegramm(GroupValueEventArgs args, DateTime timeStamp)
    {
      Args = args;
      TimeStamp = timeStamp;
    }
  }
}
