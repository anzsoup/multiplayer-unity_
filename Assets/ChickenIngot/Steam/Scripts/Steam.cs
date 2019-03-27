using System;
using UnityEngine;
using Facepunch.Steamworks;
using ChickenIngot.Networking;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor;

namespace ChickenIngot.Steam
{
	public class Steam : MonoBehaviour
	{
		private enum ConnectionOperationType
		{
			Connecting, Disconnecting
		}
		private class ConnectionOperation
		{
			public ConnectionOperationType type;
			public RMPPeer peer;
			public string version;
			public byte[] steamTicketData;
			public byte[] steamIdData;
			public string username;
			public ulong steamId;
		}

		private static Steam _instance = null;

		[SerializeField]
		private RMPNetworkView _view;
		[SerializeField]
		private uint _appId;
		[SerializeField]
		private bool _isServerOnly;
		private readonly Queue<ConnectionOperation> _operationQueue = new Queue<ConnectionOperation>();
		private ConnectionOperation _waitingUser;

		public static bool IsInitialized { get; private set; }
		public static uint AppId { get; private set; }
		public static SteamUser Me { get; private set; }
		public static List<SteamUser> Users { get; private set; }
		public static SteamConfig Config { get; set; }
		public static Client Client { get; private set; }
		public static Server Server { get; private set; }

		#region Events

		[Header("Server Side Events")]
		[SerializeField]
		private StartSteamServerEvent _onStartSteamServer;
		[SerializeField]
		private StopSteamServerEvent _onStopSteamServer;
		[SerializeField]
		private SteamUserJoinEvent _onSteamUserJoin;
		[SerializeField]
		private SteamUserExitEvent _onSteamUserExit;

		[Header("Client Side Events")]
		[SerializeField]
		private JoinSteamServerEvent _onJoinSteamServer;
		[SerializeField]
		private ExitSteamServerEvent _onExitSteamServer;

		public StartSteamServerEvent OnStartSteamServer { get { return _onStartSteamServer; } }
		public StopSteamServerEvent OnStopSteamServer { get { return _onStopSteamServer; } }
		public SteamUserJoinEvent OnSteamUserJoin { get { return _onSteamUserJoin; } }
		public SteamUserExitEvent OnSteamUserExit { get { return _onSteamUserExit; } }
		public JoinSteamServerEvent OnJoinSteamServer { get { return _onJoinSteamServer; } }
		public ExitSteamServerEvent OnExitSteamServer { get { return _onExitSteamServer; } }

		#endregion

#if UNITY_EDITOR
		[MenuItem("GameObject/Steam", priority = 30)]
		static void CreateGameObject()
		{
			var go = new GameObject("Steam", typeof(RMPNetworkView), typeof(Steam));
			var steam = go.GetComponent<Steam>();
			var view = go.GetComponent<RMPNetworkView>();
			steam._view = view;
			view.AddMessageReceiver(steam);
			Undo.RegisterCreatedObjectUndo(go, "Create Steam");
		}

		public static void SetServerOnlyOption(bool value)
		{
			if (IsInitialized)
			{
				_instance._isServerOnly = value;
			}
		}
#endif

		void Awake()
		{
			DontDestroyOnLoad(gameObject);

			if (_instance == null)
			{
				DontDestroyOnLoad(gameObject);
				_instance = this;
			}
			else
			{
				Debug.LogWarning("Steam Unity Service instance already exsists.");
				Destroy(this);
				return;
			}
		}

		void Start()
		{
			// Configure us for this unity platform
			Facepunch.Steamworks.Config.ForUnity(Application.platform.ToString());
			
			AppId = _appId;
			Me = null;
			Users = new List<SteamUser>();
			Config = new SteamConfig();

			// RMP 네트워킹을 사용중일 경우 자동으로 스팀서버가 연동된다.
			if (RMPNetworkService.IsInitialized)
			{
				RMPNetworkService.OnStartServer.AddListener(_OnServerOpen);
				RMPNetworkService.OnStopServer.AddListener(_OnServerClose);
				RMPNetworkService.OnClientConnect.AddListener(_OnClientConnect);
				RMPNetworkService.OnClientDisconnect.AddListener(_OnClientDisconnect);
				RMPNetworkService.OnDisconnectFromServer.AddListener(_OnDisconnectFromServer);
			}

			if (!_isServerOnly)
				StartSteamClient();

			IsInitialized = true;
		}

		void Update()
		{
			UpdateClient();
			UpdateServer();
			ProcessConnection();
		}

		void OnDestroy()
		{
			StopSteamClient();
			StopSteamServer();
		}

		private void UpdateClient()
		{
			if (Client == null)
				return;

			try
			{
				UnityEngine.Profiling.Profiler.BeginSample("Steam client update");
				Client.Update();
			}
			finally
			{
				UnityEngine.Profiling.Profiler.EndSample();
			}
		}

		private void UpdateServer()
		{
			if (Server == null)
				return;

			try
			{
				UnityEngine.Profiling.Profiler.BeginSample("Steam server update");
				Server.Update();
			}
			finally
			{
				UnityEngine.Profiling.Profiler.EndSample();
			}
		}

		private void ProcessConnection()
		{
			// 클라이언트 접속, 퇴장의 순서와 atomic한 연산을 보장하기 위한 큐잉처리
			if (NetworkService.IsOnline && NetworkService.IsServer)
			{
				if (_waitingUser == null && _operationQueue.Count > 0)
				{
					ConnectionOperation args = _operationQueue.Dequeue();
					if (Server == null) return;
					switch (args.type)
					{
						case ConnectionOperationType.Connecting:
							UserAuth(args);
							break;

						case ConnectionOperationType.Disconnecting:
							DisposeUser(args);
							break;
					}
				}
			}
		}

		/// <summary>
		/// 플레이어 접속을 승인할지 거부할지 결정한다.
		/// </summary>
		[ServerOnly]
		private void UserAuth(ConnectionOperation user)
		{
			string version = user.version;
			RMPPeer client = user.peer;

			if (client.Status != RMPPeer.PeerStatus.Connected)
			{
				Debug.LogWarning("User already disconnected quickly before Steam Auth started.");
				return;
			}

			// 인원수
			if (Users.Count >= Config.MaxPlayers)
			{
				string msg = "Server is full. (" + Users.Count + "/" + Config.MaxPlayers + ")";
				RejectUser(client, msg);
				return;
			}
			// 버전
			if (!version.Equals(Config.Version))
			{
				string msg = "Version mismatch. Client: " + version + ", Server: " + Config.Version;
				RejectUser(client, msg);
				return;
			}

			// 스팀인증
			var ticketData = user.steamTicketData;
			var steamID = BitConverter.ToUInt64(user.steamIdData, 0);
			var username = user.username;
			Debug.Log(string.Format("Authorizing steam user. : {0}", username));

			if (Server.Auth.StartSession(ticketData, steamID))
			{
				// 이후의 처리는 OnAuthChange 에서
				user.steamId = steamID;
				SetServerAuthWaitingStatus(user);
			}
			else
			{
				string msg = "Failed to start steam auth session.";
				RejectUser(client, msg);
				return;
			}
		}

		[ServerOnly]
		private void OnAuthChange(ulong steamId, ulong ownerId, ServerAuth.Status status)
		{
			ConnectionOperation waitingUser = _waitingUser;

			// 인증 진행중인 유저에 대한 이벤트일 때
			if (waitingUser != null && steamId == waitingUser.steamId)
			{
				// 인증 대기 목록에서 제거
				ResetServerAuthWaitingStatus();

				switch (status)
				{
					case ServerAuth.Status.OK:
						AcceptUser(waitingUser);
						break;

					case ServerAuth.Status.AuthTicketCanceled:
						Debug.LogWarning("Auth ticket canceled while doing user auth.");
						break;

					default:
						var message = string.Format("Steam auth failed. ({0}): {1}", waitingUser.username, status.ToString());
						RejectUser(waitingUser.peer, message);
						break;
				}
			}
			else
			{
				switch (status)
				{
					case ServerAuth.Status.AuthTicketCanceled:
						Debug.Log("Auth ticket canceled. (" + steamId + ")");
						break;

					default:
						Debug.Log("Steam auth changed. (" + steamId + ")");
						break;
				}
			}

		}

		[ServerOnly]
		private void AcceptUser(ConnectionOperation user)
		{
			RMPPeer client = user.peer;

			// 스팀 인증이 끝나기 전에 접속 종료한 경우
			if (client.Status != RMPPeer.PeerStatus.Connected)
			{
				Debug.LogWarning("User already disconnected while authorizing.");
				Server.Auth.EndSession(user.steamId);
				return;
			}

			var steamId = user.steamId;
			// 서버 프로그램은 직접 전달받는 것 외에는 유저의 정보를 알 방법이 없다.
			var username = user.username;
			var steamuser = new SteamUser(client, steamId, username);
			Users.Add(steamuser);

			Debug.Log(string.Format("Steam user connected. : {0}", username));

			// accept 알림
			_view.RPC(client, "clRPC_ConnectionAccepted");

			OnSteamUserJoin.Invoke(steamuser);
		}

		[ServerOnly]
		private void DisposeUser(ConnectionOperation user)
		{
			var steamuser = Users.Find(u => u.Peer == user.peer);
			if (steamuser == null)
			{
				Debug.LogError("Disconnection event invoked, but steam user not found.");
				return;
			}

			Debug.Log(string.Format("Closing steam session. : {0}", steamuser.SteamId));
			Server.Auth.EndSession(steamuser.SteamId);
			Users.Remove(steamuser);

			Debug.Log(string.Format("Steam user disconnected. : {0}", steamuser.Username));

			OnSteamUserExit.Invoke(steamuser);
		}

		[ServerOnly]
		private void RejectUser(RMPPeer peer, string message)
		{
			Debug.LogWarning("Reject steam user : " + message);
			_view.RPC(peer, "clRPC_ConnectionRejected", message);
			// disconnect는 클라에서 한다. 패킷은 비동기적으로 전송되기 때문에
			// 서버에서 하기 좀 곤란함.
			//peer.Disconnect();
		}

		Coroutine timeout = null;

		[ServerOnly]
		private void SetServerAuthWaitingStatus(ConnectionOperation args)
		{
			_waitingUser = args;
			timeout = StartCoroutine(UserAuthTimeout());
		}

		[ServerOnly]
		private void ResetServerAuthWaitingStatus()
		{
			_waitingUser = null;
			StopCoroutine(timeout);
			timeout = null;
		}

		[ServerOnly]
		IEnumerator UserAuthTimeout()
		{
			// 한 번에 하나의 유저만 처리한다고 가정
			// 두 명 이상의 유저가 대기중일 때에는 이 알고리즘을 적용할 수 없음
			var user = _waitingUser;

			yield return new WaitForSeconds(5.0f);

			// 딜레이 후 여전히 대기중일 경우 강제 취소
			if (user == _waitingUser)
			{
				string msg = "Steam user auth canceled. (Time out)";
				RejectUser(_waitingUser.peer, msg);
				Server.Auth.EndSession(_waitingUser.steamId);
				ResetServerAuthWaitingStatus();
			}
		}

		private static bool StartSteamServer(Action<ulong, ulong, ServerAuth.Status> OnAuthChange)
		{
			if (Server != null)
				return false;

			var serverInit = new ServerInit(Config.ModDir, Config.GameDescription);
			serverInit.Secure = Config.Secure;
			serverInit.VersionString = Config.Version;

			Server = new Server(_instance._appId, serverInit);
			Server.ServerName = Config.Name;
			Server.MaxPlayers = Config.MaxPlayers;
			Server.LogOnAnonymous();

			if (!Server.IsValid)
			{
				Server = null;
				Debug.LogError("Couldn't initialize steam server.");
				return false;
			}

			Server.Auth.OnAuthChange = OnAuthChange;
			Debug.Log("Steam server initialized.");
			return true;
		}

		private void StopSteamServer()
		{
			if (Server != null)
			{
				foreach (var user in Users)
					Server.Auth.EndSession(user.SteamId);

				Users.Clear();
				Server.Auth.OnAuthChange = null;
				Server.Dispose();
				Server = null;
			}
		}

		private static bool StartSteamClient()
		{
			Client = new Client(_instance._appId);

			if (!Client.IsValid)
			{
				Client = null;
				Debug.LogError("Couldn't initialize steam client.");
				return false;
			}

			Me = new SteamUser(null, Client.SteamId, Client.Username);
			Debug.Log(string.Format("Steam client initialized : {0} / {1}", Me.Username, Me.SteamId));
			return true;
		}

		private void StopSteamClient()
		{
			if (Client != null)
			{
				Client.Dispose();
				Client = null;
				Me.CancelAuthSessionTicket();
				Me = null;
			}
		}

		[RMP]
		[ClientOnly]
		private void clRPC_ConnectionAccepted()
		{
			Debug.Log("Steam server accepts connection.");
			OnJoinSteamServer.Invoke();
		}

		[RMP]
		[ClientOnly]
		private void clRPC_ConnectionRejected(string message)
		{
			Debug.LogWarning("Steam server rejects connection. : " + message);
			RMPNetworkService.StopClient();
		}

		[RMP]
		[ClientOnly]
		private void clRPC_HandShake()
		{
			Debug.Log("Getting steam auth ticket.");
			Auth.Ticket ticket = Client.Auth.GetAuthSessionTicket();
			ulong steamId = Client.SteamId;
			byte[] ticketData = ticket.Data;
			byte[] steamIDData = BitConverter.GetBytes(steamId);
			string username = Me.Username;
			// 버전, 스팀티켓, 스팀id 전송
			_view.RPC(RPCOption.ToServer, "svRPC_HandShake", Config.Version, ticketData, steamIDData, username);
		}

		[RMP]
		[ServerOnly]
		private void svRPC_HandShake(string version, byte[] steamTicketData, byte[] steamIDData, string username)
		{
			Debug.Log(string.Format("Steam auth ticket received. : {0}", username));
			ConnectionOperation req = new ConnectionOperation();
			req.type = ConnectionOperationType.Connecting;
			req.peer = _view.MessageSender;
			req.version = version;
			req.steamTicketData = steamTicketData;
			req.steamIdData = steamIDData;
			req.username = username;
			_operationQueue.Enqueue(req);
		}

		[ServerOnly]
		private void _OnServerOpen()
		{
			// 서버를 먼저 만든 다음 스팀에 올림
			if (!StartSteamServer(OnAuthChange))
			{
				Debug.LogError("Failed to initialize steam server");
				return;
			}

			OnStartSteamServer.Invoke();
		}

		[ServerOnly]
		private void _OnServerClose()
		{
			StopSteamServer();
			OnStopSteamServer.Invoke();
		}

		[ServerOnly]
		private void _OnClientConnect(RMPPeer client)
		{
			Debug.Log("Client connected. Waiting for steam auth ticket.");
			_view.RPC(client, "clRPC_HandShake");
		}

		[ServerOnly]
		private void _OnClientDisconnect(RMPPeer client)
		{
			// 다른 유저의 접속 처리 도중에 사라지면 안되기 때문에 atomic한 연산이 보장되어야 한다.
			ConnectionOperation disconnect = new ConnectionOperation();
			disconnect.type = ConnectionOperationType.Disconnecting;
			disconnect.peer = client;
			_operationQueue.Enqueue(disconnect);
		}

		[ClientOnly]
		private void _OnDisconnectFromServer(RMPPeer server)
		{
			Me.CancelAuthSessionTicket();
			OnExitSteamServer.Invoke();
		}
	}
}