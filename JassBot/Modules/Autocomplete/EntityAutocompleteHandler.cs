using Discord;
using Discord.Interactions;
using WCSharp.IO.JassDoc.Output;

namespace JassBot.Modules.Autocomplete
{
	public class EntityAutocompleteHandler : AutocompleteHandler
	{
		public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
			IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
		{
			var results = AutocompleteUtil.BasicSearch<JassEntity>(autocompleteInteraction, services);
			return Task.FromResult(AutocompleteUtil.GetResults(results));
		}
	}
}
