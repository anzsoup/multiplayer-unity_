using UnityEngine;

namespace Salgu.Networking.Demo
{
	public class DemoGUI : MonoBehaviour
	{
		[SerializeField] DemoChatting _demoChatting = null;
		string _log;
		string _chatInput;
		
		void Start()
		{
			Application.logMessageReceived += HandleLog;
		}

		void OnGUI()
		{
			Title();
			NetworkMenu();
			Log();

			if (NetworkService.IsOnline)
				Chatting();
		}

		private void Title()
		{
			string title = "<color=lime>Salgu.Networking Demo</color> ";
			if (NetworkService.IsOnline)
				if (NetworkService.IsServer)
					title += "<color=lime>[Server]</color>";
				else
					title += "<color=lime>[Client]</color>";
			else
				title += "<color=red>[Offline]</color>";

			GUILayout.Label(title);
		}

		private void NetworkMenu()
		{
			if (NetworkService.IsOnline)
			{
				if (GUILayout.Button("Quit Demo", GUILayout.Width(100)))
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
				GUILayout.Label("Start Demo");

				if (GUILayout.Button("Server", GUILayout.Width(100)))
				{
					RMPNetworkService.StartServer(22277, 10);
				}
				if (GUILayout.Button("Client", GUILayout.Width(100)))
				{
					RMPNetworkService.StartClient("127.0.0.1", 22277);
				}
			}
		}

		private void Log()
		{
			GUILayout.BeginVertical("Box");

			var labelStyle = GUI.skin.label;
			var oldStyle = new GUIStyle(labelStyle);
			labelStyle.alignment = TextAnchor.LowerLeft;

			GUILayout.Label(_log, GUILayout.Width(400), GUILayout.Height(100));

			GUI.skin.label = oldStyle;

			GUILayout.EndVertical();
		}

		private void Chatting()
		{
			GUILayout.BeginHorizontal();

			_chatInput = GUILayout.TextField(_chatInput, GUILayout.Width(300));

			if (GUILayout.Button("Send"))
			{
				if (!string.IsNullOrEmpty(_chatInput))
				{
					_demoChatting.SendChat(_chatInput);
					_chatInput = "";
				}
			}

			GUILayout.EndHorizontal();
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
	}
}