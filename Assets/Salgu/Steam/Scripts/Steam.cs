using System;
using UnityEngine;
using Facepunch.Steamworks;
using Salgu.Networking;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;

namespace Salgu.Steam
{
	/// <summary>
	/// 자동으로 스팀 클라이언트를 초기화하고 스팀서버를 관리해 준다.
	/// RMP Network Service 객체가 존재하면 서버를 열때 스팀서버가 활성화 된다.
	/// GameObject- Steam 메뉴를 선택하여 게임오브젝트를 씬에 추가할 수 있다.
	/// </summary>
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
			public ulong steamId;
			public string username;
		}

		private static Steam _instance = null;
		
		[SerializeField] RMPNetworkView _view = null;
		[Tooltip("스팀에 게임을 등록한 후 발급받은 Id를 입력한다. 없으면 그대로 둔다.")]
		[SerializeField] uint _appId = 480;
		[Tooltip("체크하면 스팀 클라이언트 초기화를 하지 않는다. 서버 빌드 시 사용한다.")]
		[SerializeField] bool _isServerOnly = false;

		
		readonly Queue<ConnectionOperation> _operationQueue = new Queue<ConnectionOperation>();
		ConnectionOperation _waitingUser;

		/// <summary>
		/// Steam 클래스에 접근할 땐 이 값이 true 인지 반드시 확인해야 한다.
		/// </summary>
		public static bool IsReady { get; private set; }

		/// <summary>
		/// 게임의 스팀 App Id.
		/// </summary>
		public static uint AppId { get; private set; }

		/// <summary>
		/// 나의 SteamUser 객체
		/// </summary>
		public static SteamUser Me { get; private set; }

		/// <summary>
		/// 서버에 접속한 스팀유저들. 내가 서버일 경우에만 유효하다.
		/// </summary>
		public static List<SteamUser> Users { get; private set; }

		/// <summary>
		/// 게임 정보 및 스팀서버 설정.
		/// </summary>
		public static SteamConfig Config { get; set; }

		/// <summary>
		/// Facepunch.Steamworks.Client 객체
		/// </summary>
		public static Client Client { get; private set; }

		/// <summary>
		/// Facepunch.Steamworks.Server 객체
		/// </summary>
		public static Server Server { get; private set; }

		#region Events

		[Header("Server Side Events")]
		[SerializeField] StartSteamServerEvent _onStartSteamServer = null;
		[SerializeField] StopSteamServerEvent _onStopSteamServer = null;
		[SerializeField] SteamUserJoinEvent _onSteamUserJoin = null;
		[SerializeField] SteamUserExitEvent _onSteamUserExit = null;

		[Header("Client Side Events")]
		[SerializeField] JoinSteamServerEvent _onJoinSteamServer = null;
		[SerializeField] ExitSteamServerEvent _onExitSteamServer = null;

		public StartSteamServerEvent OnStartSteamServer { get { return _onStartSteamServer; } }
		public StopSteamServerEvent OnStopSteamServer { get { return _onStopSteamServer; } }
		public SteamUserJoinEvent OnSteamUserJoin { get { return _onSteamUserJoin; } }
		public SteamUserExitEvent OnSteamUserExit { get { return _onSteamUserExit; } }
		public JoinSteamServerEvent OnJoinSteamServer { get { return _onJoinSteamServer; } }
		public ExitSteamServerEvent OnExitSteamServer { get { return _onExitSteamServer; } }

		#endregion

#if UNITY_EDITOR
		[MenuItem("GameObject/Salgu/Steam", priority = 30)]
		static void CreateGameObject()
		{
			var find = FindObjectOfType<Steam>();
			if (find != null)
			{
				Debug.LogError("Steam Object already exists.");
				return;
			}

			var go = new GameObject("Steam", typeof(Steam));
			var steam = go.GetComponent<Steam>();
			var view = go.GetComponent<RMPNetworkView>();
			steam._view = view;
			view.AddMessageReceiver(steam);
			Undo.RegisterCreatedObjectUndo(go, "Create Steam");
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
				Debug.LogWarning("Steam instance already exists.");
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
			if (FindObjectOfType<RMPNetworkService>() != null)
			{
				RMPNetworkService.OnStartServer.AddListener(_OnServerOpen);
				RMPNetworkService.OnStopServer.AddListener(_OnServerClose);
				RMPNetworkService.OnClientConnect.AddListener(_OnClientConnect);
				RMPNetworkService.OnClientDisconnect.AddListener(_OnClientDisconnect);
				RMPNetworkService.OnDisconnectFromServer.AddListener(_OnDisconnectFromServer);
			}

			if (!_isServerOnly)
				StartSteamClient();

			IsReady = true;
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
			var steamId = user.steamId;
			var username = user.username;
			Debug.Log(string.Format("Authorizing steam user. : {0}", username));

			if (Server.Auth.StartSession(ticketData, steamId))
			{
				// 이후의 처리는 OnAuthChange 에서
				user.steamId = steamId;
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
			string username = Me.Username;
			// 버전, 스팀티켓, 스팀id 전송
			_view.RPC(RPCOption.ToServer, "svRPC_HandShake", Config.Version, ticketData, steamId, username);
		}

		[RMP]
		[ServerOnly]
		private void svRPC_HandShake(string version, byte[] steamTicketData, ulong steamId, string username)
		{
			Debug.Log(string.Format("Steam auth ticket received. : {0}", username));
			ConnectionOperation req = new ConnectionOperation();
			req.type = ConnectionOperationType.Connecting;
			req.peer = _view.MessageSender;
			req.version = version;
			req.steamTicketData = steamTicketData;
			req.steamId = steamId;
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