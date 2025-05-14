namespace Hsp.LedStripProxy;

public interface IPackageSender
{
  Task Send(byte[] data, CancellationToken cancellationToken);
}