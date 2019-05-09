using UnityEngine;
using LLenok.Networking;

namespace LLenok.Steam.Demo
{
	public class DemoGUI : MonoBehaviour
	{
		[SerializeField]
		private string _modDir = "steam-demo";
		[SerializeField]
		private string _gameDesc = "Steam Demo";
		[SerializeField]
		private string _version = "1.0";
		[SerializeField]
		private string _name = "Steam Demo Server";
		[SerializeField]
		private int _maxPlayers = 20;
		private string _log;

		void Start()
		{
			Application.logMessageReceived += HandleLog;
		}

		void OnGUI()
		{
			Title();
			UserInfo();
			ServerTest();
			Log();
		}

		private void HandleLog(string logString, string stackTrace, LogType type)
		{
			switch (type)
			{
				case LogType.Log:
					_log += "\n" + logString;
					break;

				case LogType.Warning:
					_log += "\n<color=yellow>" + logString + "</color>";
					break;

				case LogType.Error:
					_log += "\n<color=red>" + logString + "</color>";
					break;

				case LogType.Exception:
					_log += "\n<color=red>" + logString + "</color>";
					break;
			}
		}

		private void Title()
		{
			string title = "<color=lime>LLenok.Steam Demo</color> ";
			if (NetworkService.IsOnline)
				if (NetworkService.IsServer)
					title += "<color=lime>[Server]</color>";
				else
					title += "<color=lime>[Client]</color>";
			else
				title += "<color=red>[Offline]</color>";

			GUILayout.BeginHorizontal();
			GUILayout.Label(title);
			GUILayout.Label(string.Format("AppId : {0}", Steam.AppId));
			GUILayout.EndHorizontal();
		}

		private void UserInfo()
		{
			if (Steam.Client != null)
			{
				string info = string.Format("Username : {0}", Steam.Me.Username);
				info += string.Format("\nSteam Id : {0}", Steam.Me.SteamId);

				GUILayout.Label(info);
			}
			else
			{
				GUILayout.Label("<color=red>Steam Client Not Initialized</color>");
			}
		}

		private void ServerTest()
		{
			GUILayout.Label("Steam Server Test");

			if (NetworkService.IsOnline)
			{
				if (GUILayout.Button("Quit", GUILayout.Width(100)))
				{
					if (NetworkService.IsServer)
					{
						RMPNetworkService.StopServer();
					}
					else
					{
						RMPNetworkService.StopClient();
					}
				}
			}
			else
			{
				if (GUILayout.Button("Server", GUILayout.Width(100)))
				{
					Steam.Config.ModDir = _modDir;
					Steam.Config.GameDescription = _gameDesc;
					Steam.Config.Version = _version;
					Steam.Config.Name = _name;
					Steam.Config.MaxPlayers = _maxPlayers;
					RMPNetworkService.StartServer(22277, 10);
				}
				if (Steam.Client != null)
				{
					if (GUILayout.Button("Client", GUILayout.Width(100)))
					{
						RMPNetworkService.StartClient("127.0.0.1", 22277);
					}
				}
			}
		}

		private void Log()
		{
			GUILayout.BeginVertical("Box");

			var labelStyle = GUI.skin.label;
			var oldStyle = new GUIStyle(labelStyle);
			labelStyle.alignment = TextAnchor.LowerLeft;

			GUILayout.Label(_log, GUILayout.Width(400), GUILayout.Height(200));

			GUI.skin.label = oldStyle;

			GUILayout.EndVertical();
		}
	}
}