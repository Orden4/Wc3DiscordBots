using Discord;
using Discord.Interactions;
using WCSharp.IO.JassDoc.Output;

namespace JassBot.Modules.Autocomplete
{
	public class NativeAutocompleteHandler : AutocompleteHandler
	{
		public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
			IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
		{
			var results = AutocompleteUtil.BasicSearch<JassMethod>(autocompleteInteraction, services)
				.Where(x => x.SourceFile.Contains("common.j", StringComparison.OrdinalIgnoreCase));
			return Task.FromResult(AutocompleteUtil.GetResults(results));
		}
	}
}
