#if UNITY_EDITOR

using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Salgu.Build
{
	public static class Symbol
	{
		private static IEnumerable<string> _currentSymbols;

		private static readonly BuildTargetGroup[] buildTargetGroup = new[]
		{
			BuildTargetGroup.Standalone,
		};

		public static string CurrentSymbols
		{
			get { return string.Join(";", _currentSymbols.ToArray()); }
		}

		static Symbol()
		{
			_currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';');
		}

		private static void SaveSymbol()
		{
			var symbols = CurrentSymbols;
			foreach (var target in buildTargetGroup)
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(target, symbols);
			}

		}

		public static void Add(params string[] symbols)
		{
			_currentSymbols = _currentSymbols.Concat(symbols).Distinct().ToArray();
			SaveSymbol();
		}

		public static void Remove(params string[] symbols)
		{
			_currentSymbols = _currentSymbols.Except(symbols).ToArray();
			SaveSymbol();
		}

		public static void Set(params string[] symbols)
		{
			_currentSymbols = symbols;
			SaveSymbol();
		}

		public static bool Contains(string symbol)
		{
			return _currentSymbols.Contains(symbol);
		}
	}
}

#endif