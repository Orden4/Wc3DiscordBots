using System.Diagnostics.CodeAnalysis;
using WCSharp.IO.JassDoc.Output;

namespace JassBot.Services.Interfaces
{
	public interface IJassDoc
	{
		JassApi Api { get; }

		T Get<T>(string name) where T : JassEntity;
		IEnumerable<T> GetByInfix<T>(string infix);
		IEnumerable<T> GetByPrefix<T>(string prefix);
		bool TryGet<T>(string name, [NotNullWhen(true)] out T? value) where T : JassEntity;
	}
}
