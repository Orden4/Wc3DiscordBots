using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using JassBot.Modules;
using JassBot.Services;
using JassBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace JassBot
{
	internal class Program
	{
		private static async Task Main()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

			var appBuilder = Host.CreateApplicationBuilder();

			appBuilder.Configuration
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddUserSecrets<Program>();

			appBuilder.Services.Configure<AppSettings>(appBuilder.Configuration);

			appBuilder.Services
				// General
				.AddSerilog(x => GetDefaultLogger(appBuilder, x))

				// Singletons
				.AddSingleton(GetDiscordSocketConfig)
				.AddSingleton(GetInteractionServiceConfig)
				.AddSingleton<DiscordSocketClient>()
				.AddSingleton<DiscordRestClient>(x => x.GetRequiredService<DiscordSocketClient>().Rest)
				.AddSingleton<InteractionService>()
				.AddSingleton<IJassDoc, JassDoc>()

				// Hosted
				.AddHostedService<DiscordStartup>()
				.AddHostedService<ReloadModule>()
			;

			using var app = appBuilder.Build();
			await app.RunAsync();
		}

		private static LoggerConfiguration GetDefaultLogger(HostApplicationBuilder appBuilder, LoggerConfiguration x)
		{
			return x.ReadFrom.Configuration(appBuilder.Configuration);
		}

		private static DiscordSocketConfig GetDiscordSocketConfig(IServiceProvider serviceProvider)
		{
			return new()
			{
				GatewayIntents = GatewayIntents.AllUnprivileged
			};
		}

		private static InteractionServiceConfig GetInteractionServiceConfig(IServiceProvider serviceProvider)
		{
			return new()
			{
				DefaultRunMode = RunMode.Async,
				UseCompiledLambda = true,
			};
		}
	}
}
