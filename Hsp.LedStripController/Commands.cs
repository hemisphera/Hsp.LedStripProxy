using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReaSharp;
using ReaSharp.Models;

namespace Hsp.LedStripController;

public class Commands
{
  /// <summary>
  /// Command ID assigned by REAPER for the <c>HSP_LEDCONTROLLER_TOGGLE</c> action.
  /// Set from <see cref="Plugin"/> after registration so the toggleaction
  /// callback can report the resulting on/off state back to REAPER, which makes
  /// the action behave as a toolbar toggle button / checked Actions list entry.
  /// </summary>
  public static int? ToggleCommandId { get; set; }


  public static async Task Start(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StartAsync();
    RefreshToolbarState(arg);
  }

  public static async Task Stop(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.StopAsync();
    RefreshToolbarState(arg);
  }

  public static async Task Toggle(IServiceProvider arg, ActionContext context)
  {
    var dispatcher = arg.GetRequiredService<GmemToOscDispatcher>();
    await dispatcher.ToggleAsync();
    RefreshToolbarState(arg);
  }


  private static void RefreshToolbarState(IServiceProvider arg)
  {
    if (!ToggleCommandId.HasValue)
    {
      var logger = arg.GetService<ILogger<Commands>>();
      logger?.LogWarning("Toggle command ID not set; cannot refresh toolbar state in REAPER.");
      return;
    }

    // The on/off state itself is reported via the toggleaction callback
    // registered in Plugin.cs (REAPER queries it on demand). Here we just ask
    // REAPER to refresh the toolbar button so it reflects the new state
    // immediately after a Start/Stop/Toggle.
    try
    {
      Reaper.RefreshToolbar.Invoke(ToggleCommandId.Value);
    }
    catch (NotSupportedException)
    {
      // RefreshToolbar is unavailable in this REAPER version; ignore.
    }
  }
}