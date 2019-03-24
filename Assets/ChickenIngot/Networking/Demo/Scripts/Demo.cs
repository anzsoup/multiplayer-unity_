using UnityEngine;

namespace ChickenIngot.Networking.Demo
{
	/// -----------------------------------------------------------------------------------
	/// [Network Events]
	///		RMPUnityService 에 메소드를 등록하여 네트워크 이벤트를 사용할 수 있다.
	/// -----------------------------------------------------------------------------------

	public class Demo : MonoBehaviour
	{
		[SerializeField]
		private DemoReplicate _demoSpawning;

		void Awake()
		{
			_demoSpawning.enabled = false;
		}

		public void _OnClientConnect(RMPPeer client)
		{
			_demoSpawning.enabled = true;
			Debug.LogWarning("\t** Client Connected **");
		}

		public void _OnClientDisconnect(RMPPeer client)
		{
			_demoSpawning.enabled = false;
			Debug.LogWarning("\t** Client Disconnected **");
		}

		public void _OnServerOpen()
		{
			Debug.LogWarning("\t** Server Open **");
		}

		public void _OnServerClose()
		{
			_demoSpawning.enabled = false;
			Debug.LogWarning("\t** Server Closed **");
		}

		public void _OnConnectToServer(RMPPeer server)
		{
			_demoSpawning.enabled = true;
			Debug.LogWarning("\t** Connected **");
		}

		public void _OnDisconnectFromServer()
		{
			_demoSpawning.enabled = false;
			Debug.LogWarning("\t** Disconnected **");
		}
	}
}