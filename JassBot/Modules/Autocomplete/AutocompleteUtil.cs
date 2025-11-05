using Discord;
using Discord.Interactions;
using JassBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using WCSharp.IO.JassDoc.Output;

namespace JassBot.Modules.Autocomplete
{
	public static class AutocompleteUtil
	{
		public static IEnumerable<T> BasicSearch<T>(IAutocompleteInteraction autocompleteInteraction, IServiceProvider services)
			where T : JassEntity
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(userInput))
				return [];

			var doc = services.GetRequiredService<IJassDoc>();
			IEnumerable<T> suggestions;

			var userInputLower = userInput.ToLower();
			suggestions = doc.GetByPrefix<T>(userInputLower);
			suggestions = suggestions.Concat(doc.GetByInfix<T>(userInputLower).Except(suggestions));

			return suggestions;
		}

		public static AutocompletionResult GetResults(this IEnumerable<JassEntity> entities)
		{
			var results = new List<AutocompleteResult>();
			foreach (var entity in entities.Take(25))
			{
				var identifier = entity.GetOrigin() switch
				{
					EntityOrigin.Blizzard => "(BJ) ",
					EntityOrigin.AI => "(AI) ",
					_ => "",
				};
				results.Add(new AutocompleteResult($"{identifier}{entity.Name}", entity.Name));
			}
			return AutocompletionResult.FromSuccess(results);
		}
	}
}
