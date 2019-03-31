#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ChickenIngot.Steam.Editor
{
	[InitializeOnLoad]
	public static class SteamBuild
	{

		private static bool _prefsLoaded;
		private static bool _copyDlls;
		private static bool _copyServerDlls;

		static SteamBuild()
		{
			_copyDlls = EditorPrefs.GetBool("multiplayer-unity.Steam.CopyDlls", true);
			_copyServerDlls = EditorPrefs.GetBool("multiplayer-unity.Steam.CopyServerDlls", true);
		}

		[PreferenceItem("Steam")]
		static void PreferencesGUI()
		{
			_copyDlls = EditorGUILayout.Toggle("Copy Dlls on Build", _copyDlls);
			if (_copyDlls) _copyServerDlls = EditorGUILayout.Toggle("Copy Server Dlls on Build", _copyServerDlls);

			if (GUI.changed)
			{
				EditorPrefs.SetBool("multiplayer-unity.Steam.CopyDlls", _copyDlls);
				EditorPrefs.SetBool("multiplayer-unity.Steam.CopyServerDlls", _copyServerDlls);
			}
		}

		[PostProcessBuild(0)]
		public static void Copy(BuildTarget target, string pathToBuiltProject)
		{
			if (!_copyDlls)
				return;

			//
			// Only steam
			//
			if (!target.ToString().StartsWith("Standalone"))
				return;

			//
			// You only need a steam_appid.txt if you're launching outside of Steam, you don't need to ship with it
			// but most games do anyway.
			//
			FileUtil.ReplaceFile("steam_appid.txt", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steam_appid.txt");

			//
			// Put these dlls next to the exe
			//
			if (target == BuildTarget.StandaloneWindows)
				FileUtil.ReplaceFile("steam_api.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steam_api.dll");

			if (target == BuildTarget.StandaloneWindows64)
				FileUtil.ReplaceFile("steam_api64.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steam_api64.dll");

			if (target == BuildTarget.StandaloneOSX)
				FileUtil.ReplaceFile("libsteam_api.dylib", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/libsteam_api.dylib");

			if (target == BuildTarget.StandaloneLinux64 || target == BuildTarget.StandaloneLinuxUniversal)
				FileUtil.ReplaceFile("libsteam_api64.so", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/libsteam_api64.so");

			if (target == BuildTarget.StandaloneLinux || target == BuildTarget.StandaloneLinuxUniversal)
				FileUtil.ReplaceFile("libsteam_api.so", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/libsteam_api.so");

			if (_copyServerDlls)
			{
				//
				// You need these dlls to run server app without steam
				//
				if (target == BuildTarget.StandaloneWindows)
				{
					FileUtil.ReplaceFile("steamclient.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steamclient.dll");
					FileUtil.ReplaceFile("tier0_s.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/tier0_s.dll");
					FileUtil.ReplaceFile("vstdlib_s.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/vstdlib_s.dll");
				}

				if (target == BuildTarget.StandaloneWindows64)
				{
					FileUtil.ReplaceFile("steamclient64.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steamclient64.dll");
					FileUtil.ReplaceFile("tier0_s64.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/tier0_s64.dll");
					FileUtil.ReplaceFile("vstdlib_s64.dll", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/vstdlib_s64.dll");
				}

				if (target == BuildTarget.StandaloneLinux || target == BuildTarget.StandaloneLinuxUniversal)
				{
					FileUtil.ReplaceFile("steamclient.so", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/steamclient.so");
				}

				if (target == BuildTarget.StandaloneLinux64 || target == BuildTarget.StandaloneLinuxUniversal)
				{
					FileUtil.ReplaceDirectory("linux64", System.IO.Path.GetDirectoryName(pathToBuiltProject) + "/linux64");
				}
			}
		}
	}
}

#endif