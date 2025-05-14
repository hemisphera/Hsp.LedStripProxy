using System.Reflection;
using Hsp.LedStripProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
  .ConfigureAppConfiguration((hostContext, cb) =>
  {
    var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
    cb.SetBasePath(basePath);
    cb.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    cb.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
  })
  .ConfigureServices((hostContext, services) =>
  {
    services.Configure<Settings>(hostContext.Configuration);
    services.AddSingleton<IPackageSender, UdpPackageSender>();
    services.AddLogging();
    services.AddHostedService<LedStripDispatcher>();
  })
  .RunConsoleAsync();