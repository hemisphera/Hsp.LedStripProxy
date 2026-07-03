using Microsoft.Extensions.DependencyInjection;
using ReaSharp.Models;

namespace Hsp.LedStripController;

public class Commands
{
  public static async Task Start(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StartAsync();
  }

  public static async Task Stop(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StopAsync();
  }
}