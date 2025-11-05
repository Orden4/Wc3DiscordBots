using Discord;
using WCSharp.IO.JassDoc.Output;

namespace JassBot
{
	public enum EntityOrigin
	{
		Common,
		Blizzard,
		AI,
		Unknown,
	}

	public static class EntityOriginExtensions
	{
		public static EntityOrigin GetOrigin(this JassEntity entity)
		{
			if (string.Equals(entity.SourceFile, "common.j", StringComparison.OrdinalIgnoreCase))
			{
				return EntityOrigin.Common;
			}
			else if (string.Equals(entity.SourceFile, "blizzard.j", StringComparison.OrdinalIgnoreCase))
			{
				return EntityOrigin.Blizzard;
			}
			else if (string.Equals(entity.SourceFile, "common.ai", StringComparison.OrdinalIgnoreCase))
			{
				return EntityOrigin.AI;
			}
			return EntityOrigin.Unknown;
		}

		public static Color GetColor(this JassEntity entity)
		{
			return entity.GetOrigin() switch
			{
				EntityOrigin.Common => Color.DarkGreen,
				EntityOrigin.Blizzard => entity is JassMethod ? Color.DarkRed : Color.Orange,
				EntityOrigin.AI => Color.Gold,
				_ => Color.Red,
			};
		}
	}
}
