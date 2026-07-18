using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hsp.LedStripController.Programs;
using Hsp.Osc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReaSharp;

namespace Hsp.LedStripController;

public static class Plugin
{
  /// <summary>
  /// Resolved once at load time so the <see cref="ToggleAction"/> callback
  /// (invoked by REAPER on the main thread to query on/off state) can read
  /// <see cref="GmemToOscDispatcher.IsRunning"/> without going through DI on
  /// every call.
  /// </summary>
  private static GmemToOscDispatcher? Dispatcher { get; set; }


  [UnmanagedCallersOnly(EntryPoint = "ReaperPluginEntry")]
  public static int ReaperPluginEntry(nint hInstance, nint rec)
  {
    var host = Host.CreateDefaultBuilder()
      .ConfigureLogging((context, lb) =>
      {
        lb.ClearProviders();
        lb.SetMinimumLevel(LogLevel.Warning);
#if DEBUG
        lb.SetMinimumLevel(LogLevel.Debug);
#endif
        lb.AddConfiguration(context.Configuration.GetSection("Logging"));
        lb.AddProvider(new ReaperConsoleLoggerProvider());
      })
      .ConfigureServices((context, sc) =>
      {
        sc.AddSingleton<ICommandRegistry, DefaultCommandRegistry>();
        sc.AddSingleton<GmemService>();
        // The OSC client is created fresh on each dispatcher start (via the
        // factory below) so that the underlying UDP socket is rebound to the
        // current network interface. Reusing one socket for the plugin lifetime
        // would keep it bound to whatever network was active when the plugin
        // loaded, so a WiFi change would not take effect until REAPER restarts.
        sc.AddSingleton<Func<IOscClient>>(c => () => new OscUdpClient(9100));
        sc.AddSingleton<LedStripProgramRegistry>();
        sc.AddSingleton<GmemToOscDispatcher>();
      })
      .Build();

    try
    {
      var state = PluginState.Initialize(ReaperPluginInfo.FromPointer(rec), host);
      var commands = host.Services.GetRequiredService<ICommandRegistry>();

      // Register LED strip programs (program number -> implementation).
      // Speed is controlled live via the generic argument (MIDI Note 16
      // velocity), so each pattern/direction variant is registered only once.
      var programRegistry = host.Services.GetRequiredService<LedStripProgramRegistry>();
      programRegistry.Register<ColorCycleProgram>(1);
      programRegistry.Register<PulseProgram>(2);
      programRegistry.Register(3, () => new RandomProgram());
      programRegistry.Register(4, () => new FallProgram(false));
      programRegistry.Register(5, () => new FallProgram(true));
      programRegistry.Register(6, () => new ExpandProgram(true));
      programRegistry.Register(7, () => new ExpandProgram(false));

      commands.Register("HSP_LEDCONTROLLER_START", "LED Controller: Start", Commands.Start);
      commands.Register("HSP_LEDCONTROLLER_STOP", "LED Controller: Stop", Commands.Stop);
      var toggleCommand = commands.Register("HSP_LEDCONTROLLER_TOGGLE", "LED Controller: Toggle", Commands.Toggle);
      Commands.ToggleCommandId = toggleCommand.Id;

      // Register a toggleaction callback so REAPER treats the Toggle command as
      // a real toggle action: the Actions list shows a checkmark and a toolbar
      // button stays pressed while the dispatcher is running. SetToggleCommandState
      // only works for ReaScripts (per the REAPER docs), so the canonical mechanism
      // for native/custom actions is to answer REAPER's toggleaction queries.
      Dispatcher = host.Services.GetRequiredService<GmemToOscDispatcher>();
      RegisterToggleAction();
      return 1;
    }
    catch (Exception ex)
    {
      return 0;
    }
  }


  private static void RegisterToggleAction()
  {
    unsafe
    {
      // int (*)(int command) — returns -1 (not ours), 0 (off), 1 (on).
      var togglePtr = (IntPtr)(delegate* unmanaged[Cdecl]<int, int>)&ToggleAction;
      var toggleName = Marshal.StringToHGlobalAnsi("toggleaction");
      Reaper.Register(toggleName, togglePtr);
      Marshal.FreeHGlobal(toggleName);
    }
  }

  /// <summary>
  /// Queried by REAPER to obtain the on/off state of a registered action.
  /// Returning 0/1 for our Toggle command makes it behave as a toolbar toggle
  /// button / checked Actions list entry; -1 for anything else means "not ours".
  /// </summary>
  [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
  private static int ToggleAction(int command)
  {
    var id = Commands.ToggleCommandId;
    if (!id.HasValue || command != id.Value)
      return -1;

    return Dispatcher?.IsRunning == true ? 1 : 0;
  }
}