using Microsoft.Extensions.DependencyInjection;

namespace Hsp.LedStripController;

public class Commands
{
  public static async Task Start(IServiceProvider arg)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StartAsync();
  }

  public static async Task Stop(IServiceProvider arg)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StopAsync();
  }
}