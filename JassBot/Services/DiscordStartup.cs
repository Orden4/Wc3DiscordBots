using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace JassBot.Services
{
	public class DiscordStartup : BackgroundService
	{
		private readonly IServiceProvider serviceProvider;
		private readonly IOptions<AppSettings> appSettings;
		private readonly DiscordSocketClient discordSocketClient;
		private readonly InteractionService interactionService;
		private readonly ILogger logger;

		public DiscordStartup(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings, DiscordSocketClient discordSocketClient,
			InteractionService interactionService, ILogger logger)
		{
			this.serviceProvider = serviceProvider;
			this.appSettings = appSettings;
			this.discordSocketClient = discordSocketClient;
			this.interactionService = interactionService;
			this.logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this.interactionService.Log += InteractionService_Log;
			this.discordSocketClient.Log += DiscordSocketClient_Log;
			this.discordSocketClient.Ready += DiscordSocketClient_Ready;
			this.discordSocketClient.InteractionCreated += DiscordSocketClient_InteractionCreated;

			await this.discordSocketClient.LoginAsync(TokenType.Bot, this.appSettings.Value.Token);
			await this.discordSocketClient.StartAsync();
		}

		private Task InteractionService_Log(LogMessage arg)
		{
			if (arg.Exception != null)
				this.logger.Error(arg.Exception, "Interactions encountered an exception.");

			return Task.CompletedTask;
		}

		private Task DiscordSocketClient_Log(LogMessage arg)
		{
			if (arg.Exception != null)
				this.logger.Error(arg.Exception, "Discord encountered an exception.");

			return Task.CompletedTask;
		}

		private async Task DiscordSocketClient_Ready()
		{
			await this.interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), this.serviceProvider);
			await this.interactionService.RegisterCommandsGloballyAsync();
		}

		private async Task DiscordSocketClient_InteractionCreated(SocketInteraction arg)
		{
			var ctx = new SocketInteractionContext(this.discordSocketClient, arg);
			await this.interactionService.ExecuteCommandAsync(ctx, this.serviceProvider);
		}
	}
}
