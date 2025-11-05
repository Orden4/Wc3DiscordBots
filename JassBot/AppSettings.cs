namespace JassBot
{
	public class AppSettings
	{
		public string Token { get; set; } = null!;
		public Uri JassBotUri { get; set; } = null!;
		public Uri[] JassUris { get; set; } = [];
	}
}
