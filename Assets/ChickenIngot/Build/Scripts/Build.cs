#if UNITY_EDITOR

using System.IO;
using UnityEditor;

namespace ChickenIngot.Build
{
	/// <summary>
	/// 빌드 시스템은 현재 64비트 윈도우 환경에 최적화 되어있다.
	/// Facepunch.Steamworks 에 문제가 있어서 32비트 환경에선 서버가 동작하지 않고,
	/// 리눅스 빌드는 아직 고려하고 있지 않기 때문이다.
	/// 따라서 빌드 시스템을 통해 빌드할 경우 모든 빌드 세팅이 무시되므로 유의.
	/// </summary>
	[InitializeOnLoad]
	public static class Build
	{
		private enum EditorMode
		{
			None,
			Server,
			Client,
		}

		public const string SERVER_SYMBOL = "SERVER_APP";
		public const string CLIENT_SYMBOL = "CLIENT_APP";
		private const string EDITOR_PREFS_EDITOR_MODE = "multiplayer-unity.Build.EditorMode";
		private const string MENU_SERVER_MODE = "Build/Editor Mode/Server Mode";
		private const string MENU_CLIENT_MODE = "Build/Editor Mode/Client Mode";
		private const string MENU_BUILD_SERVER_WINDOWS = "Build/Build Windows Server";
		private const string MENU_BUILD_CLIENT_WINDOWS = "Build/Build Windows Client";

		private static EditorMode _editorMode;

		static Build()
		{
			_editorMode = (EditorMode)EditorPrefs.GetInt(EDITOR_PREFS_EDITOR_MODE);

			if (_editorMode == EditorMode.None)
				ChangeClientMode();

			EditorApplication.delayCall += SetMenuChecked;
		}

		[MenuItem(MENU_SERVER_MODE, priority = 10)]
		static void ChangeServerMode()
		{
			if (_editorMode != EditorMode.Server)
			{
				SetServerSymbol();
				_editorMode = EditorMode.Server;
				EditorPrefs.SetInt(EDITOR_PREFS_EDITOR_MODE, (int)_editorMode);
				SetMenuChecked();
			}
		}

		[MenuItem(MENU_CLIENT_MODE, priority = 10)]
		static void ChangeClientMode()
		{
			if (_editorMode != EditorMode.Client)
			{
				SetClientSymbol();
				_editorMode = EditorMode.Client;
				EditorPrefs.SetInt(EDITOR_PREFS_EDITOR_MODE, (int)_editorMode);
				SetMenuChecked();
			}
		}

		[MenuItem(MENU_BUILD_SERVER_WINDOWS)]
		static void BuildWindowsServer()
		{
			var prevSymbols = Symbol.CurrentSymbols;
			SetServerSymbol();
			
			string path = GetServerBuildDirectory() + "server.exe";
			
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, 
				BuildTarget.StandaloneWindows64, BuildOptions.None);

			WriteBatchFile();

			Symbol.Set(prevSymbols.Split(';'));
		}

		[MenuItem(MENU_BUILD_CLIENT_WINDOWS)]
		static void BuildWindowsClient()
		{
			var prevSymbols = Symbol.CurrentSymbols;
			SetClientSymbol();
			
			string path = GetClientBuildDirectory() + "client.exe";
			
			BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, 
				BuildTarget.StandaloneWindows64, BuildOptions.None);

			Symbol.Set(prevSymbols.Split(';'));
		}

		private static void SetServerSymbol()
		{
			bool svSymbol = Symbol.Contains(SERVER_SYMBOL);
			bool clSymbol = Symbol.Contains(CLIENT_SYMBOL);

			if (!svSymbol)
				Symbol.Add(SERVER_SYMBOL);

			if (clSymbol)
				Symbol.Remove(CLIENT_SYMBOL);
		}

		private static void SetClientSymbol()
		{
			bool svSymbol = Symbol.Contains(SERVER_SYMBOL);
			bool clSymbol = Symbol.Contains(CLIENT_SYMBOL);

			if (svSymbol)
				Symbol.Remove(SERVER_SYMBOL);

			if (!clSymbol)
				Symbol.Add(CLIENT_SYMBOL);
		}

		private static void SetMenuChecked()
		{
			switch (_editorMode)
			{
				case EditorMode.None:
					Menu.SetChecked(MENU_SERVER_MODE, false);
					Menu.SetChecked(MENU_CLIENT_MODE, false);
					break;

				case EditorMode.Server:
					Menu.SetChecked(MENU_SERVER_MODE, true);
					Menu.SetChecked(MENU_CLIENT_MODE, false);
					break;

				case EditorMode.Client:
					Menu.SetChecked(MENU_SERVER_MODE, false);
					Menu.SetChecked(MENU_CLIENT_MODE, true);
					break;
			}
		}

		private static string GetServerBuildDirectory()
		{
			return string.Format("Build/Server/{0} {1} {2}/", 
				PlayerSettings.productName, BuildInfo.VERSION, "Server");
		}

		private static string GetClientBuildDirectory()
		{
			return string.Format("Build/Client/{0} {1}/",
				PlayerSettings.productName, BuildInfo.VERSION);
		}

		private static void WriteBatchFile()
		{
			string content = File.ReadAllText("run.bat.txt");
			content = string.Format(content, PlayerSettings.productName + ".exe");

			File.WriteAllText(GetServerBuildDirectory() + "/run.bat", content);
		}
	}
}

#endif