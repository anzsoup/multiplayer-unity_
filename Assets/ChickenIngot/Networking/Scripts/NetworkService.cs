using UnityEngine;
using UnityEngine.Networking;
using System;

namespace ChickenIngot.Networking
{
	/// <summary>
	/// 유니티 네트워킹 LLAPI 를 이용해 서버나 클라이언트를 열고 패킷을 주고받는 기능이 구현되어 있다.
	/// 원격 사용자와의 프로토콜은 서버나 클라이언트를 시작할 때 전달해 주는 IPeer 객체에 정의한다.
	/// </summary>
	public class NetworkService : MonoBehaviour
	{
		private static NetworkService _instance = null;
		private static INetworkEventHandler _handler = null;
		private byte[] _buffer = new byte[Packet.BUFFER_LENGTH];

		public static bool IsServer { get; private set; }
		public static bool IsOnline { get; private set; }

		void Awake()
		{
			if (_instance == null)
			{
				DontDestroyOnLoad(gameObject);
				_instance = this;
			}
			else
			{
				Debug.LogError("Network Service instance already exists.");
				Destroy(this);
				return;
			}
		}

		void Start()
		{
			NetworkTransport.Init();

			// 오프라인 상태일 땐 자신이 서버라고 가정하면 일관성 있는 코드를 작성할 수 있다.
			IsServer = true;
			IsOnline = false;
		}

		void Update()
		{
			// StartServer나 StartClient가 호출되어 초기화되기 전엔 아래의 코드를 실행하지 않음
			if (_handler == null) return;

			// 핸들러가 모든 리소스를 정리하고 끝마칠 준비가 돼야 제거한다.
			// 서버나 클라이언트를 종료하고 난 후에도 처리해야할 이벤트가 있을 수 있기 때문이다.
			if (_handler.IsDead())
			{
				_handler.OnRemoved();
				_handler = null;
				IsOnline = false;
				IsServer = true;
				return;
			}	
			
			// Notice we process all network events until we get a 'Nothing' response here.
			// Often people just process a single event per frame, and that results in very poor performance.
			var noEventsLeft = false;
			while (!noEventsLeft)
			{
				byte errorCode;
				int dataSize;
				int channelId;
				int connectionId;
				int hostId;

				var netEventType = NetworkTransport.Receive(
				  out hostId,
				  out connectionId,
				  out channelId,
				  _buffer,
				  _buffer.Length,
				  out dataSize,
				  out errorCode);

				var error = (NetworkError)errorCode;
				if (error != NetworkError.Ok)
				{
					Debug.Log(string.Format("NetworkTransport error : {0}", error));
					_handler.OnError(hostId, connectionId, channelId, error);
					return;
				}

				switch (netEventType)
				{
					case NetworkEventType.Nothing:
						noEventsLeft = true;
						break;
					case NetworkEventType.ConnectEvent:
						_handler.OnConnectEvent(hostId, connectionId, channelId);
						break;
					case NetworkEventType.DataEvent:
						_handler.OnDataEvent(hostId, connectionId, channelId, _buffer, dataSize);
						break;
					case NetworkEventType.DisconnectEvent:
						_handler.OnDisconnectEvent(hostId, connectionId, channelId);
						break;
				}
			}
		}

		void OnDestroy()
		{
			_instance = null;

			if (_handler != null)
				_handler.Stop();

			NetworkTransport.Shutdown();
			IsOnline = false;
		}

		private static bool CheckInited()
		{
			if (_instance == null)
			{
				Debug.LogError("NetworkService has not been initialized. You need to call NetworkService.Init() first.");
				return false;
			}

			return true;
		}

		/// <summary>
		/// 스크립트 인스턴스를 하나 만든다. 이미 존재할 경우 아무일도 하지 않는다.
		/// </summary>
		/// <param name="obj">null 일경우 새 GameObject를 만든다. 아닐경우 해당 GameObject에 스크립트를 붙인다.</param>
		public static void Init(GameObject obj = null)
		{
			if (_instance == null)
			{
				if (obj == null)
				{
					new GameObject("Network Service", typeof(NetworkService));
				}
				else
				{
					obj.AddComponent<NetworkService>();
				}
			}
		}

		public static void StartServer<Protocol>(int localPort = 0, int maxConnection = 1) where Protocol : IPeer, new()
		{
			if (CheckInited())
			{
				if (IsOnline)
				{
					if (IsServer)
						Debug.LogWarning("You are already a server. StartServer() is aborted.");
					else
						Debug.LogWarning("You are already a client. StartServer() is aborted.");
				}

				_handler = new EventHandlerServer<Protocol>(localPort, maxConnection);
				IsOnline = true;
			}
		}

		public static void EndServer()
		{
			if (CheckInited())
			{
				if (!IsOnline)
				{
					Debug.LogWarning("You are not currently in online state. EndServer() is aborted.");
				}
				else if (!IsServer)
				{
					Debug.LogWarning("You are not a server. EndServer() is aborted.");
				}
				
				_handler.Stop();
			}
		}

		public static void StartClient<Protocol>(string remoteHost, int remotePort) where Protocol : IPeer, new()
		{
			if (CheckInited())
			{
				if (IsOnline)
				{
					if (IsServer)
						Debug.LogWarning("You are already a server. StartClient() is aborted.");
					else
						Debug.LogWarning("You are already a client. StartClient() is aborted.");
				}

				_handler = new EventHandlerClient<Protocol>(remoteHost, remotePort, () => { IsOnline = true; IsServer = false; });
			}
		}

		public static void EndClient()
		{
			if (CheckInited())
			{
				if (!IsOnline)
				{
					Debug.LogWarning("You are not currently in online state. EndClient() is aborted.");
				}
				else if (IsServer)
				{
					Debug.LogWarning("You are not a client. EndClient() is aborted.");
				}
				
				_handler.Stop();
			}
		}

		public static void Send(int hostId, int connectionId, Packet msg, QosType channel = NetworkConfig.DefaultChannel)
		{
			if (hostId < 0 || connectionId < 0) return;
			if (!IsOnline) return;

			try
			{
				byte error;
				NetworkTransport.Send(hostId, connectionId, NetworkConfig.Channels[channel], msg.Buffer, msg.Size, out error);
				if ((NetworkError)error != NetworkError.Ok)
				{
					Debug.LogWarning(string.Format("Failed to send. : {0}", (NetworkError)error));
				}
			}
			catch (Exception error)
			{
				Debug.LogError(error);
			}
		}
	}
}