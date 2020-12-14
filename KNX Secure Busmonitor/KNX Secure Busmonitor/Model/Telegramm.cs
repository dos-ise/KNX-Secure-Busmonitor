using System;
using System.Collections.Generic;
using System.Text;

namespace Busmonitor.Model
{
  using Knx.Bus.Common;
  using System.Globalization;

  public class Telegramm
  {
    public GroupValueEventArgs Args { get; }

    public string GroupName { get; set; }

    public string DisplayNameValue { get; }

    public DateTime TimeStamp { get; }

    public string RAW => GetRaw();

    private string GetRaw()
    {
      byte[] data = Args.Value.Value;
      var dataLength = KnxHelper.GetDataLength(data);

      // HEADER
      var header = new byte[6];
      header[0] = 0x06;
      header[1] = 0x10;
      header[2] = 0x05;
      header[3] = 0x30;
      var totalLength = BitConverter.GetBytes(dataLength + 16);
      header[4] = totalLength[1];
      header[5] = totalLength[0];
      //TODO

      var result = CreateActionDatagramCommon("1/1/16", data, header);
      //return ByteArrayToString(result);
      var source = ByteArrayToString(KnxHelper.GetAddress(Args.Address));
      var target = ByteArrayToString(KnxHelper.GetAddress(Args.IndividualAddress));
      return string.Format("290A0604928E67E5800241E2BCE0{0}{1}{2}0081", target, source, ByteArrayToString(Args.Value.Value));
    }

    public string ByteArrayToString(byte[] ba)
    {
      StringBuilder hex = new StringBuilder(ba.Length * 2);
      foreach (byte b in ba)
        hex.AppendFormat("{0:x2}", b);
      return hex.ToString();
    }

    protected byte[] CreateActionDatagramCommon(string destinationAddress, byte[] data, byte[] header)
    {
      int i;
      var dataLength = KnxHelper.GetDataLength(data);

      // HEADER
      var datagram = new byte[dataLength + 10 + header.Length];
      for (i = 0; i < header.Length; i++)
        datagram[i] = header[i];

      // CEMI (start at position 6)
      // +--------+--------+--------+--------+----------------+----------------+--------+----------------+
      // |  Msg   |Add.Info| Ctrl 1 | Ctrl 2 | Source Address | Dest. Address  |  Data  |      APDU      |
      // | Code   | Length |        |        |                |                | Length |                |
      // +--------+--------+--------+--------+----------------+----------------+--------+----------------+
      //   1 byte   1 byte   1 byte   1 byte      2 bytes          2 bytes       1 byte      2 bytes
      //
      //  Message Code    = 0x11 - a L_Data.req primitive
      //      COMMON EMI MESSAGE CODES FOR DATA LINK LAYER PRIMITIVES
      //          FROM NETWORK LAYER TO DATA LINK LAYER
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          | Data Link Layer Primitive | Message Code | Data Link Layer Service | Service Description | Common EMI Frame |
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          |        L_Raw.req          |    0x10      |                         |                     |                  |
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          |                           |              |                         | Primitive used for  | Sample Common    |
      //          |        L_Data.req         |    0x11      |      Data Service       | transmitting a data | EMI frame        |
      //          |                           |              |                         | frame               |                  |
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          |        L_Poll_Data.req    |    0x13      |    Poll Data Service    |                     |                  |
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          |        L_Raw.req          |    0x10      |                         |                     |                  |
      //          +---------------------------+--------------+-------------------------+---------------------+------------------+
      //          FROM DATA LINK LAYER TO NETWORK LAYER
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          | Data Link Layer Primitive | Message Code | Data Link Layer Service | Service Description |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |        L_Poll_Data.con    |    0x25      |    Poll Data Service    |                     |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |                           |              |                         | Primitive used for  |
      //          |        L_Data.ind         |    0x29      |      Data Service       | receiving a data    |
      //          |                           |              |                         | frame               |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |        L_Busmon.ind       |    0x2B      |   Bus Monitor Service   |                     |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |        L_Raw.ind          |    0x2D      |                         |                     |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |                           |              |                         | Primitive used for  |
      //          |                           |              |                         | local confirmation  |
      //          |        L_Data.con         |    0x2E      |      Data Service       | that a frame was    |
      //          |                           |              |                         | sent (does not mean |
      //          |                           |              |                         | successful receive) |
      //          +---------------------------+--------------+-------------------------+---------------------+
      //          |        L_Raw.con          |    0x2F      |                         |                     |
      //          +---------------------------+--------------+-------------------------+---------------------+

      //  Add.Info Length = 0x00 - no additional info
      //  Control Field 1 = see the bit structure above
      //  Control Field 2 = see the bit structure above
      //  Source Address  = 0x0000 - filled in by router/gateway with its source address which is
      //                    part of the KNX subnet
      //  Dest. Address   = KNX group or individual address (2 byte)
      //  Data Length     = Number of bytes of data in the APDU excluding the TPCI/APCI bits
      //  APDU            = Application Protocol Data Unit - the actual payload including transport
      //                    protocol control information (TPCI), application protocol control
      //                    information (APCI) and data passed as an argument from higher layers of
      //                    the KNX communication stack
      //

      //TODO Action MessageCode
      datagram[i++] = (byte)0x11;

      datagram[i++] = 0x00;
      datagram[i++] = 0xAC;

      datagram[i++] =
          KnxHelper.IsAddressIndividual(destinationAddress)
              ? (byte)0x50
              : (byte)0xF0;

      datagram[i++] = 0x00;
      datagram[i++] = 0x00;
      var dst_address = KnxHelper.GetAddress(destinationAddress);
      datagram[i++] = dst_address[0];
      datagram[i++] = dst_address[1];
      datagram[i++] = (byte)dataLength;
      datagram[i++] = 0x00;
      datagram[i] = 0x80;

      KnxHelper.WriteData(datagram, data, i);

      return datagram;
    }

    private int GetDataLength(byte[] data)
    {
      if (data.Length <= 0)
        return 0;

      if (data.Length == 1 && data[0] < 0x3F)
        return 1;

      if (data[0] < 0x3F)
        return data.Length;

      return data.Length + 1;
    }

    public Telegramm(GroupValueEventArgs args, DateTime timeStamp)
    {
      Args = args;
      TimeStamp = timeStamp;
      DisplayNameValue = ConvertToDisplayName(args);
    }

    private string ConvertToDisplayName(GroupValueEventArgs args)
    {
      if (args.Value.SizeInBit < 8)
      {
        return args.Value.Value[0].ToString();
      }
      var hex = args.Value.Value.AsHexString();
      var provider = CultureInfo.InvariantCulture;
      if (int.TryParse(hex, NumberStyles.HexNumber, provider, out int intValue))
      {
        return intValue.ToString();
      }
      else
      {
        return hex;
      }
    }
  }
}
