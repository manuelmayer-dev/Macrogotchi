using MacroDeck.Plugin.Hosting;
using MacroDeck.Plugin.Serilog;
using Macrogotchi;
using Microsoft.Extensions.DependencyInjection;

var plugin = MacroDeckPlugin.CreatePlugin(args)
	.UseMacroDeckLogging()
	.UseLocalization(Strings.LocalizationCatalog)
	.ConfigureServices((_, services) =>
	{
		services.AddSingleton<PetGame>();
		services.AddHostedService(provider => provider.GetRequiredService<PetGame>());
	})
	.RegisterIntegration<PluginIntegration>()
	.Build();

await plugin.RunAsync();
