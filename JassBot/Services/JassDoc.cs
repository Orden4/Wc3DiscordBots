using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Gma.DataStructures.StringSearch;
using JassBot.Services.Interfaces;
using Microsoft.Extensions.Options;
using WCSharp.IO.JassDoc.Output;
using WCSharp.IO.JassDoc.Parsing;

namespace JassBot.Services
{
	public partial class JassDoc : IJassDoc
	{
		public JassApi Api { get; }

		private readonly FrozenDictionary<string, JassEntity> lookup;
		private readonly FrozenDictionary<string, JassEntity> lookupCaseSensitive;
		private readonly PatriciaTrie<JassEntity> prefixTrie;
		private readonly PatriciaSuffixTrie<JassEntity> infixTrie;

		public JassDoc(IOptions<AppSettings> appSettings)
		{
			Api = JassDocParser.ParseDocAsync(appSettings.Value.JassUris).GetAwaiter().GetResult();

			var excessiveWhitespacesCode = ExcessiveWhitespacesCode();
			foreach (var entity in Api.Entities)
			{
				entity.Description = Clean(entity.Description);
				entity.SourceCode = string.Join('\n', entity.SourceCode.Split('\n').Select(x => excessiveWhitespacesCode.Replace(x, " ")));
				for (var i = 0; i < entity.Bugs.Count; i++)
				{
					entity.Bugs[i] = Clean(entity.Bugs[i]);
				}
				for (var i = 0; i < entity.Notes.Count; i++)
				{
					entity.Notes[i] = Clean(entity.Notes[i]);
				}
			}

			foreach (var method in Api.Methods)
			{
				for (var i = 0; i < method.Parameters.Count; i++)
				{
					var parameter = method.Parameters[i];
					if (parameter.Description != null && parameter.Description.Contains('\n'))
					{
						parameter.Description = Clean(parameter.Description);
					}
				}
			}

			this.lookup = Api.Entities.DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToFrozenDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
			this.lookupCaseSensitive = Api.Entities.ToFrozenDictionary(x => x.Name, x => x);

			this.infixTrie = new(3);
			this.prefixTrie = new();
			foreach (var entity in Api.Entities)
			{
				var nameLower = entity.Name.ToLower();
				this.infixTrie.Add(nameLower, entity);
				this.prefixTrie.Add(nameLower, entity);
			}
		}

		public T Get<T>(string name) where T : JassEntity
		{
			return TryGet<T>(name, out var value)
				? value
				: throw new ArgumentException($"Cannot find an entity named '{name}'", nameof(name));
		}

		public bool TryGet<T>(string name, [NotNullWhen(true)] out T? value) where T : JassEntity
		{
			if (this.lookupCaseSensitive.TryGetValue(name, out var entity))
			{
				if (entity is T exact)
				{
					value = exact;
					return true;
				}
			}

			if (this.lookup.TryGetValue(name, out entity))
			{
				if (entity is T exact)
				{
					value = exact;
					return true;
				}
			}

			value = default;
			return false;
		}

		public IEnumerable<T> GetByPrefix<T>(string prefix)
		{
			return this.prefixTrie.Retrieve(prefix).OfType<T>();
		}

		public IEnumerable<T> GetByInfix<T>(string infix)
		{
			return this.infixTrie.Retrieve(infix).OfType<T>();
		}

		[return: NotNullIfNotNull(nameof(description))]
		private string? Clean(string? description)
		{
			if (description == null)
				return null;

			var tokens = description.Trim().Split("```");

			// Clean normal stuff
			var newLineReducer = NewLineReducer();

			for (var i = 0; i < tokens.Length; i += 2)
			{
				var textBlock = tokens[i];
				var matches = newLineReducer.Matches(textBlock);
				foreach (var match in matches.Reverse())
				{
					var seperator = match.ValueSpan.Count('\n') > 1
						? "\n"
						: " ";
					textBlock = string.Concat(textBlock.AsSpan(0, match.Index), seperator, textBlock.AsSpan(match.Index + match.Length));
				}

				if (textBlock.Length > 0)
				{
					textBlock = char.ToUpper(textBlock[0]) + textBlock.Substring(1);
				}

				tokens[i] = textBlock;
			}

			// Clean code blocks
			var langRegex = LangRegex();

			for (var i = 1; i < tokens.Length; i += 2)
			{
				var codeBlock = tokens[i];
				var langMatch = langRegex.Match(codeBlock);
				if (langMatch.Success)
				{
					codeBlock = string.Concat(langMatch.Groups[1].ValueSpan, codeBlock.AsSpan(langMatch.ValueSpan.Length));
				}

				tokens[i] = codeBlock;
			}

			return string.Join("```", tokens);
		}

		[GeneratedRegex(@"(?<![`\n\|\*-])\n(?![`\n\|\*-])|\n{2,}")]
		private static partial Regex NewLineReducer();
		[GeneratedRegex(@"^{\.([^}]*)}")]
		private static partial Regex LangRegex();
		[GeneratedRegex(@"(?<!^)\s{2,}")]
		private static partial Regex ExcessiveWhitespacesCode();
	}
}
