namespace JassBot.Extensions
{
	public static class StringExtensions
	{
		public static string CapField(this string field)
		{
			return field.Length > 1024 ? "Too long to display on Discord. See the online JassBot." : field;
		}
	}
}
