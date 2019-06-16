using System;
using UnityEditor;
using UnityEngine;

namespace Salgu.Networking
{
	/// <summary>
	/// RMP 통신을 위해서는 이 스크립트가 붙은 게임오브젝트가 존재해야 한다.
	/// GameObject - RMP Network Service 메뉴를 선택하여 게임오브젝트를 씬에 추가할 수 있다.
	/// </summary>
	public class RMPNetworkService : MonoBehaviour
	{
		private static RMPNetworkService _instance = null;

		[SerializeField]
		RMPNetworkView[] _replicateTable = null;

		public static bool IsInitialized { get; private set; }
		public static RMPNetworkView[] ReplicateTable { get { return _instance._replicateTable; } }

		#region Events

		[Header("Server Side Events")]
		[SerializeField] StartServerEvent _onStartServer = null;
		[SerializeField] StopServerEvent _onStopServer = null;
		[SerializeField] ClientConnectEvent _onClientConnect = null;
		[SerializeField] ClientDisconnectEvent _onClientDisconnect = null;

		[Header("Client Side Events")]
		[SerializeField] ConnectToServerEvent _onConnectToServer = null;
		[SerializeField] DisconnectFromServerEvent _onDisconnectFromServer = null;

		public static StartServerEvent OnStartServer { get { return _instance._onStartServer; } }
		public static StopServerEvent OnStopServer { get { return _instance._onStopServer; } }
		public static ClientConnectEvent OnClientConnect { get { return _instance._onClientConnect; } }
		public static ClientDisconnectEvent OnClientDisconnect { get { return _instance._onClientDisconnect; } }
		public static ConnectToServerEvent OnConnectToServer { get { return _instance._onConnectToServer; } }
		public static DisconnectFromServerEvent OnDisconnectFromServer { get { return _instance._onDisconnectFromServer; } }

		#endregion

#if UNITY_EDITOR
		[MenuItem("GameObject/Salgu/RMP Network Service", priority = 30)]
		static void CreateGameObject()
		{
			var find = FindObjectOfType<RMPNetworkService>();
			if (find != null)
			{
				Debug.LogError("RMP Network Service Object already exists.");
				return;
			}

			var go = new GameObject("RMP Network Service", typeof(RMPNetworkService));
			Undo.RegisterCreatedObjectUndo(go, "Create RMP Network Service");
		}
#endif

		void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
			}
			else
			{
				Debug.LogWarning("RMP Network Service instance already exists.");
				Destroy(this);
				return;
			}	
		}

		void Start()
		{
			// 인스펙터에 프리팹을 끌어다놓은 것들은 모두 하나의 프리팹을 참조하므로
			// 이런식으로 프리팹의 데이터를 수정하는 것이 유효함.
			for (int i = 0; i < _replicateTable.Length; ++i)
			{
				var view = _replicateTable[i];
				view.ReplicationTableIndex = i;
				view.Guid = "";
			}

			NetworkService.Init(gameObject);
			IsInitialized = true;
		}

		void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
				IsInitialized = false;
			}
		}

		public static void StartServer(int localPort = 0, int maxConnection = 1)
		{
			NetworkService.StartServer<RMPPeerClient>(localPort, maxConnection);
			OnStartServer.Invoke();
		}

		public static void StopServer()
		{
			NetworkService.StopServer();
			OnStopServer.Invoke();
		}

		public static void StartClient(string remoteHost, int remotePort)
		{
			NetworkService.StartClient<RMPPeerServer>(remoteHost, remotePort);
		}

		public static void StopClient()
		{
			NetworkService.StopClient();
		}

		/// <summary>
		/// 해당 이름을 가진 프리팹을 Replication Table 에서 찾아서 복제한다.
		/// 복제된 오브젝트들은 view 를 통해 RMP 통신이 가능하다.
		/// </summary>
		/// <param name="to">null 이면 모두에게, 아니면 해당 세션에게만.</param>
		[ServerOnly]
		public static RMPNetworkView Replicate(string prefabName, RMPPeer to = null)
		{
			if (!NetworkService.IsServer)
			{
				Debug.LogError("Only server can replicate RMP object. Replication aborted.");
				return null;
			}

			var target = System.Array.Find(_instance._replicateTable, prefab => prefab.name.Equals(prefabName));

			if (target != null)
			{
				// view 의 Awake 가 호출되고 새 guid 가 부여됨.
				var instance = Instantiate(target);
				// 직렬화 되지 않는 멤버변수는 instantiate 할 때 복사되지 않음.
				instance.ReplicationTableIndex = target.ReplicationTableIndex;

				// null 일 경우 모두에게, 아닐 경우 해당 세션에게만.
				if (to == null)
				{
					var cls = RMPPeer.ClientPeers;
					if (cls != null)
					{
						foreach (var peer in cls.Values)
							peer.SendReplicate(instance);
					}
				}
				else
				{
					to.SendReplicate(instance);
				}
				
				return instance;
			}
			else
			{
				Debug.LogError(string.Format("Replicate target not found by its name \'{0}\'.", prefabName));
				return null;
			}
		}

		/// <summary>
		/// Replication Table 에서 해당 인덱스의 프리팹을 복제한다.
		/// 복제된 오브젝트들은 view 를 통해 RMP 통신이 가능하다.
		/// </summary>
		/// <param name="to">null 이면 모두에게, 아니면 해당 세션에게만.</param>
		[ServerOnly]
		public static RMPNetworkView Replicate(int index, RMPPeer to = null)
		{
			if (!NetworkService.IsServer)
			{
				Debug.LogError("Only server can replicate RMP object. Replication aborted.");
				return null;
			}
			
			try
			{
				var target = _instance._replicateTable[index];
				var instance = Instantiate(target);
				instance.ReplicationTableIndex = target.ReplicationTableIndex;

				if (to == null)
				{
					var cls = RMPPeer.ClientPeers;
					if (cls != null)
					{
						foreach (var peer in cls.Values)
							peer.SendReplicate(instance);
					}
				}
				else
				{
					to.SendReplicate(instance);
				}
				
				return instance;
			}
			catch (Exception error)
			{
				Debug.LogError(error);
				return null;
			}
		}

		/// <summary>
		/// 해당 오브젝트를 복제한다.
		/// 복제된 오브젝트들은 view 를 통해 RMP 통신이 가능하다.
		/// </summary>
		/// <param name="to">null 이면 모두에게, 아니면 해당 세션에게만.</param>
		[ServerOnly]
		public static void Replicate(RMPNetworkView view, RMPPeer to = null)
		{
			if (!NetworkService.IsServer)
			{
				Debug.LogError("Only server can replicate RMP object. Replication aborted.");
				return;
			}

			if (to == null)
			{
				var cls = RMPPeer.ClientPeers;
				if (cls != null)
				{
					foreach (var peer in cls.Values)
						peer.SendReplicate(view);
				}
			}
			else
			{
				to.SendReplicate(view);
			}
		}
	}
}