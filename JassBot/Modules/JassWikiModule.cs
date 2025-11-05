using System.Text;
using Discord;
using Discord.Interactions;
using JassBot.Extensions;
using JassBot.Modules.Autocomplete;
using JassBot.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WCSharp.IO.JassDoc.Output;

namespace JassBot.Modules
{
	[CommandContextType(InteractionContextType.Guild)]
	[Group("jass", "Look up natives and other data within JassDoc")]
	public class JassWikiModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly IHostApplicationLifetime applicationLifetime;
		private readonly IJassDoc doc;
		private readonly IOptionsMonitor<AppSettings> appSettings;

		public JassWikiModule(IHostApplicationLifetime applicationLifetime, IJassDoc doc, IOptionsMonitor<AppSettings> appSettings)
		{
			this.applicationLifetime = applicationLifetime;
			this.doc = doc;
			this.appSettings = appSettings;
		}

		[SlashCommand("reload", "Reloads all JassDoc data")]
		[RequireUserPermission(ChannelPermission.ManageMessages)]
		public async Task ReloadAsync()
		{
			await RespondAsync(text: "Reloading!");
			this.applicationLifetime.StopApplication();
		}

		[SlashCommand("native", "Search all native functions (Common)")]
		public async Task GetNativeAsync([Summary("Name", "The name of the native"), Autocomplete<NativeAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassMethod>(name, out var method) || method.HasBody)
			{
				await RespondAsync(text: $"No function found with the name '{name}'");
				return;
			}

			var embedBuilder = Format(method);
			await RespondAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("bj", "Search all BJ functions (Blizzard)")]
		public async Task GetBjAsync([Summary("Name", "The name of the BJ"), Autocomplete<BjAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassMethod>(name, out var method) || !method.HasBody)
			{
				await RespondAsync(text: $"No function found with the name '{name}'");
				return;
			}

			var embedBuilder = Format(method);
			await RespondAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("function", "Search all functions (Common, Blizzard, AI)")]
		public async Task GetFunctionAsync([Summary("Name", "The name of the function"), Autocomplete<FunctionAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassMethod>(name, out var method))
			{
				await RespondAsync(text: $"No function found with the name '{name}'");
				return;
			}

			var embedBuilder = Format(method);
			await RespondAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("type", "Search type info")]
		public async Task GetTypeAsync([Summary("Name", "The name of the type"), Autocomplete<TypeAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassType>(name, out var type))
			{
				await RespondAsync(text: $"No type found with the name '{name}'");
				return;
			}

			var embedBuilder = Format(type);
			await RespondAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("value", "Search all variable/constant info (Common, Blizzard, AI)")]
		public async Task GetValueAsync([Summary("Name", "The name of the value"), Autocomplete<PropertyAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassProperty>(name, out var property))
			{
				await RespondAsync(text: $"No property found with the name '{name}'");
				return;
			}

			var embedBuilder = Format(property);
			await RespondAsync(embed: embedBuilder.Build());
		}

		[SlashCommand("search", "Search all JASS info")]
		public async Task GetAsync([Summary("Name", "The name of the entity"), Autocomplete<EntityAutocompleteHandler>()] string name)
		{
			if (!this.doc.TryGet<JassEntity>(name, out var entity))
			{
				await RespondAsync(text: $"No entity found with the name '{name}'");
				return;
			}

			var embedBuilder = entity switch
			{
				JassMethod method => Format(method),
				JassType type => Format(type),
				JassProperty property => Format(property),
				_ => throw new Exception($"Unknown entity type: {entity.GetType().FullName}"),
			};
			await RespondAsync(embed: embedBuilder.Build());
		}

		private EmbedBuilder Format(JassMethod method)
		{
			var embedBuilder = new EmbedBuilder();
			embedBuilder.WithUrl(this.appSettings.CurrentValue.JassBotUri + method.Name);
			embedBuilder.WithTitle($"{method.Name} ({method.SourceFile})");
			embedBuilder.WithColor(method.GetColor());
			if (method.Description != null)
				embedBuilder.WithDescription(method.Description.CapField());

			if (method.Parameters.Count > 0)
			{
				if (method.Parameters.Any(x => x.Description != null && x.Description.Contains('\n')))
				{
					foreach (var parameter in method.Parameters)
					{
						embedBuilder.AddField($"{parameter.Type} — {parameter.Name}", parameter.Description?.CapField() ?? "");
					}
				}
				else
				{
					var parameters = new StringBuilder();
					for (var i = 0; i < method.Parameters.Count; i++)
					{
						var parameter = method.Parameters[i];
						if (parameter.Description != null)
						{
							parameters.AppendLine($"{i + 1}. **{parameter.Type}** — **{parameter.Name}** —  {parameter.Description}");
						}
						else
						{
							parameters.AppendLine($"{i + 1}. **{parameter.Type}** — **{parameter.Name}**");
						}
					}
					embedBuilder.AddField("Parameters", parameters.ToString().CapField());
				}
			}

			embedBuilder.AddField("Returns", method.ReturnType, inline: true);
			embedBuilder.AddField("Patch", method.Patch, inline: true);
			if (method.IsAsync)
			{
				embedBuilder.AddField("Async?", "Yes", inline: true);
			}
			if (method.IsPure && false)
			{
				embedBuilder.AddField("Pure?", "Yes", inline: true);
			}

			if (method.Events.Count > 0)
			{
				embedBuilder.AddField("Events", string.Join(", ", method.Events), inline: method.Events.Count == 1);
			}

			foreach (var bug in method.Bugs)
			{
				embedBuilder.AddField("Bug", bug.CapField());
			}
			foreach (var note in method.Notes)
			{
				embedBuilder.AddField("Note", note.CapField());
			}

			//embedBuilder.AddField("Source", $"```{method.Source}```".CapField());

			return embedBuilder;
		}

		private EmbedBuilder Format(JassType type)
		{
			var embedBuilder = new EmbedBuilder();
			embedBuilder.WithTitle($"({type.SourceFile}) {type.Name}");
			embedBuilder.WithUrl(this.appSettings.CurrentValue.JassBotUri + type.Name);
			embedBuilder.WithColor(type.GetColor());
			if (type.Description != null)
				embedBuilder.WithDescription(type.Description.CapField());

			embedBuilder.AddField("Patch", type.Patch);
			//embedBuilder.AddField("Source", $"`{type.Source}`");

			foreach (var bug in type.Bugs)
			{
				embedBuilder.AddField("Bug", bug.CapField());
			}
			foreach (var note in type.Notes)
			{
				embedBuilder.AddField("Note", note.CapField());
			}

			return embedBuilder;
		}

		private EmbedBuilder Format(JassProperty property)
		{
			var embedBuilder = new EmbedBuilder();
			embedBuilder.WithTitle($"({property.SourceFile}) {property.Name}");
			embedBuilder.WithUrl(this.appSettings.CurrentValue.JassBotUri + property.Name);
			embedBuilder.WithColor(property.GetColor());
			if (property.Description != null)
				embedBuilder.WithDescription(property.Description.CapField());

			embedBuilder.AddField("Type", $"{property.Type}{(property.IsArray ? " array" : "")}", inline: true);
			if (!property.IsArray)
			{
				embedBuilder.AddField(property.IsConstant ? "Constant" : "Initial value", property.Value ?? "N/A", inline: true);
			}
			embedBuilder.AddField("Patch", property.Patch, inline: true);
			//embedBuilder.AddField("Source", $"`{property.Source}`");

			foreach (var bug in property.Bugs)
			{
				embedBuilder.AddField("Bug", bug.CapField());
			}
			foreach (var note in property.Notes)
			{
				embedBuilder.AddField("Note", note.CapField());
			}

			return embedBuilder;
		}
	}
}
