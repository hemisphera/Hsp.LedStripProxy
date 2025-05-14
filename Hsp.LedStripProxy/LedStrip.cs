using System.Drawing;
using System.Net.Sockets;
using Hsp.Midi;
using Hsp.Midi.Messages;

namespace Hsp.LedStripProxy;

public class LedStrip
{
  public byte Index
  {
    get
    {
      lock (_buffer)
      {
        return _buffer[0];
      }
    }
  }

  public Color Color
  {
    get
    {
      lock (_buffer)
      {
        return Color.FromArgb(255, _buffer[1], _buffer[2], _buffer[3]);
      }
    }
  }

  public const int LedCount = 12;
  public const int BufferSize = LedCount + 4;
  private double _multiplier = 1.0;

  private readonly byte[] _buffer = new byte[BufferSize];
  private readonly byte[] _sendBuffer = new byte[BufferSize];


  public LedStrip(byte index)
  {
    ChangeColor(Color.FromArgb(255, 255, 255, 255));
    lock (_buffer)
    {
      _buffer[0] = index;
    }
  }

  public void ProcessMessage(IMidiMessage message)
  {
    if (message is not ChannelMessage cm) return;
    if (cm.Channel != Index) return;

    if (BitMultiplier(cm)) return;
    if (BitOn(cm)) return;
    if (BitOff(cm)) return;
    if (ChangeColor(cm)) return;
  }

  private bool ChangeColor(ChannelMessage cm)
  {
    if (cm.Command != ChannelCommand.NoteOn) return false;
    if (cm.Data1 is < 12 or > 14) return false;
    var col = Color;
    var newColor = Color.FromArgb(
      255,
      cm.Data1 == 14 ? cm.Data2 * 2 : col.R,
      cm.Data1 == 13 ? cm.Data2 * 2 : col.G,
      cm.Data1 == 12 ? cm.Data2 * 2 : col.B
    );
    return ChangeColor(newColor);
  }

  public bool ChangeColor(Color newColor)
  {
    lock (_buffer)
    {
      _buffer[1] = newColor.R;
      _buffer[2] = newColor.G;
      _buffer[3] = newColor.B;
    }

    return true;
  }

  private bool BitOn(ChannelMessage msg)
  {
    return msg.Command == ChannelCommand.NoteOn && BitOn((byte)msg.Data1, (byte)(msg.Data2 * 2));
  }

  private bool BitOff(ChannelMessage msg)
  {
    return msg.Command == ChannelCommand.NoteOff && BitOff((byte)msg.Data1);
  }

  public bool BitOn(byte bitNo, byte value)
  {
    if (bitNo > LedCount - 1) return false;
    lock (_buffer)
    {
      _buffer[bitNo + 4] = value;
    }

    return true;
  }

  public bool BitOff(byte bitNo)
  {
    if (bitNo > LedCount - 1) return false;
    lock (_buffer)
    {
      _buffer[bitNo + 4] = 0;
    }

    return true;
  }

  private bool BitMultiplier(ChannelMessage msg)
  {
    if (msg is not { Command: ChannelCommand.Controller, Data1: 110 }) return false;
    _multiplier = msg.Data2 / 127.0;
    return true;
  }

  public async Task Send(IPackageSender sender, CancellationToken ct)
  {
    lock (_buffer)
    {
      for (var i = 0; i < BufferSize; i++)
      {
        _sendBuffer[i] =
          i < 4
            ? _buffer[i]
            : (byte)(_buffer[i] * _multiplier);
      }
    }

    await sender.Send(_sendBuffer, ct);
  }
}