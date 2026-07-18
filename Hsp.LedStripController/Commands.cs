using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;

namespace Hsp.LedStripController;

public class Commands
{
  /// <summary>
  /// Command ID assigned by REAPER for the <c>HSP_LEDCONTROLLER_TOGGLE</c> action.
  /// Set from <see cref="Plugin"/> after registration so the <see cref="Toggle"/>
  /// handler can report the resulting on/off state back to REAPER via
  /// <c>SetToggleCommandState</c>, which makes the action behave as a toolbar
  /// toggle button / checked menu entry.
  /// </summary>
  public static int? ToggleCommandId { get; set; }


  public static async Task Start(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StartAsync();
    SetToggleState(arg, true);
  }

  public static async Task Stop(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StopAsync();
    SetToggleState(arg, false);
  }

  public static async Task Toggle(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    var isRunning = await dispatcher.ToggleAsync();
    SetToggleState(arg, isRunning);
  }


  private static void SetToggleState(IServiceProvider arg, bool isRunning)
  {
    if (!ToggleCommandId.HasValue)
    {
      var logger = arg.GetService<ILogger<Commands>>();
      logger?.LogWarning("Toggle command ID not set; cannot report toggle state to REAPER.");
      return;
    }

    try
    {
      Reaper.SetToggleCommandState.Invoke(0, ToggleCommandId.Value, isRunning ? 1 : 0);
    }
    catch (NotSupportedException)
    {
      // SetToggleCommandState is unavailable in this REAPER version; ignore.
    }
  }
}