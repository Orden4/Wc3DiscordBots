using Microsoft.Extensions.Hosting;

namespace JassBot.Modules
{
	public class ReloadModule : BackgroundService
	{
		private readonly IHostApplicationLifetime applicationLifetime;

		public ReloadModule(IHostApplicationLifetime applicationLifetime)
		{
			this.applicationLifetime = applicationLifetime;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var midnight = DateTime.UtcNow.AddDays(1).Date;
			var timeUntilMidnight = midnight - DateTime.UtcNow;
			await Task.Delay(timeUntilMidnight, stoppingToken);
			this.applicationLifetime.StopApplication();
		}
	}
}
