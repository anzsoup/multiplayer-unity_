using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace Salgu.Networking
{
	public enum RPCOption
	{
		/// <summary>
		/// 서버에게 보낸다. 클라이언트만 사용 가능.
		/// </summary>
		ToServer,

		/// <summary>
		/// 모든 클라이언트에게 보낸다. 서버만 사용 가능.
		/// </summary>
		Broadcast,
	}

	/// <summary>
	/// 이 컴포넌트가 붙은 게임오브젝트는 원격머신에 존재하는 페어-게임오브젝트와 RMP 통신이 가능하다.
	/// </summary>
	[ExecuteInEditMode]
	public class RMPNetworkView : MonoBehaviour
	{
		private static readonly Dictionary<string, RMPNetworkView> _viewDict = new Dictionary<string, RMPNetworkView>();

		// 서로 다른 클라이언트에서 동일한 오브젝트를 식별하기 위한 키.
		[SerializeField] [ReadOnly]
		private string _guid;
		[SerializeField]
		private MonoBehaviour[] _messageReceivers;
		private MessageCache _messageCache;
		
		public string Guid { get { return _guid; } set { _guid = value; } }

		/// <summary>
		/// RMPNetworkService 에 의해 부여된다.
		/// </summary>
		public int ReplicationTableIndex { get; set; }

		/// <summary>
		/// 현재 실행중인 메소드를 호출한 peer.
		/// 메소드 내에서 접근하면 해당 메소드를 호출한 peer를 알 수 있다.
		/// </summary>
		public RMPPeer MessageSender { get; private set; }

		public bool IsServer { get { return RMPNetworkService.IsServer; } }
		public bool IsOnline { get { return RMPNetworkService.IsOnline; } }

		void Awake()
		{
			// 에디터에서 플레이모드가 아닐 때 실행되는 걸 방지
			if (!Application.isPlaying) return;

			_messageCache = new MessageCache(_messageReceivers);
			MessageSender = null;

			if (string.IsNullOrEmpty(_guid))
			{
				// 프리팹으로부터 생성된 경우이다.
				// RMPNetworkService 가 Replicate Table 에서 프리팹의 guid 를 다 지우기 때문에 알 수 있다.

				// 서버는 동적 생성된 view 에 무조건 새 guid 를 부여한다.
				if (NetworkService.IsServer)
				{
					_guid = System.Guid.NewGuid().ToString();
					Add();
				}
			}
			else
			{
				// 씬에 이미 존재하던 경우이다.
				// RMP 통신이 가능하도록 딕셔너리에 추가해야 한다.
				Add();
			}
		}

		void OnDestroy()
		{
			// 에디터에서 플레이모드가 아닐 때 실행되는 걸 방지
			if (!Application.isPlaying) return;
			if (!NetworkService.IsOnline) return;

			Remove();

			// 복제된 오브젝트가 서버에서 제거되면 클라에서도 더 이상 쓸모가 없기때문에
			// 무조건 함께 제거한다.
			if (NetworkService.IsOnline && NetworkService.IsServer)
			{
				var cls = RMPPeer.ClientPeers;
				if (cls != null)
				{
					foreach (var cl in cls.Values)
					{
						cl.SendRemove(this);
					}
				}
			}
		}

#if UNITY_EDITOR
		// 에디터에서 view의 guid를 자동으로 관리해 주는 코드
		[SerializeField] [HideInInspector]
		int instanceID = 0;
		void Start()
		{
			if (Application.isPlaying) return;

			// 참고 : 인스턴스 id는 에디터를 재시작해도 보존된다.

			// 처음 생성되어 아직 id를 모르는 상태
			if (instanceID == 0)
			{
				instanceID = GetInstanceID();
				_guid = System.Guid.NewGuid().ToString();
			}
			// 처음 생성된 건 아닌데 어째선지 guid가 비어있을 때
			else if (string.IsNullOrEmpty(_guid))
			{
				_guid = System.Guid.NewGuid().ToString();
			}
			else
			{
				// 에디터에서 복제된 경우 또는 프리팹으로부터 생성된 경우
				// 유효한 인스턴스 id를 가지고 있지만 instanceID 필드에 기록된 것과 다를 때
				if (instanceID != GetInstanceID() && GetInstanceID() < 0)
				{
					// Duplicated!!
					instanceID = GetInstanceID();
					_guid = System.Guid.NewGuid().ToString();
					
					// 프리팹 인스턴스는 변경된 상태가 저장이 안되는 문제가 있는데
					// 프리팹 연결을 끊는 것 외에는 해결책이 없다.
					UnityEditor.PrefabUtility.DisconnectPrefabInstance(gameObject);

				}
			}
		}

		public void AddMessageReceiver(MonoBehaviour receiver)
		{
			if (receiver == null) return;

			if (_messageReceivers == null)
			{
				_messageReceivers = new MonoBehaviour[1];
				_messageReceivers[0] = receiver;
			}
			else
			{
				var length = _messageReceivers.Length;
				var newArray = new MonoBehaviour[length + 1];
				Array.Copy(_messageReceivers, newArray, length);
				newArray[length] = receiver;
				_messageReceivers = newArray;
			}
		}
#endif

		private void Add()
		{
			_viewDict.Add(_guid, this);
		}

		private void Remove()
		{
			_viewDict.Remove(_guid);
		}

		private MethodInfo GetMethodFrom(int receiverIndex, string message)
		{
			return _messageCache.Access(receiverIndex, message);
		}

		public static RMPNetworkView Get(string guid)
		{
			return _viewDict[guid];
		}

		[ClientOnly]
		public void ReceiveGuid(string guid)
		{
			_guid = guid;
			Add();
		}

		/// <summary>
		/// 이 뷰의 Message Receiver 에 등록된 스크립트에서 메소드를 호출한다.
		/// </summary>
		/// <param name="sender">메시지를 보낸 peer</param>
		/// <param name="methodName">호출할 메소드</param>
		/// <param name="parameters">메소드에 넘겨줄 인수를 순서대로 입력</param>
		public void SendReflectionMessage(RMPPeer sender, string methodName, params object[] parameters)
		{
			if (_messageReceivers == null) return;

			for (int i = 0; i < _messageReceivers.Length; ++i)
			{
				MethodInfo method = GetMethodFrom(i, methodName);
				if (method != null)
				{
					// receiver 가 null 이면 method 가 null 이므로
					// receiver는 null 체크를 안해도 됨
					MessageSender = sender;
					method.Invoke(_messageReceivers[i], parameters);
					MessageSender = null;
				}
			}
		}

		/// <summary>
		/// 해당 채널을 통해 RPC 메시지를 보낸다.
		/// 상대측의 페어 게임오브젝트의 메소드가 호출된다.
		/// </summary>
		public void RPC(RMPPeer to, QosType channel, string methodName, params object[] parameters)
		{
			to.SendRPC(this, channel, methodName, parameters);
		}

		/// <summary>
		/// 기본 채널을 통해 RPC 메시지를 보낸다.
		/// 상대측의 페어 게임오브젝트의 메소드가 호출된다.
		/// </summary>
		public void RPC(RMPPeer to, string methodName, params object[] parameters)
		{
			RPC(to, NetworkConfig.DefaultChannel, methodName, parameters);
		}

		public void RPC(RPCOption option, QosType channel, string methodName, params object[] parameters)
		{
			switch (option)
			{
				case RPCOption.ToServer:
					RPC_ToServer(channel, methodName, parameters);
					break;

				case RPCOption.Broadcast:
					RPC_Broadcast(channel, methodName, parameters);
					break;
			}
		}

		public void RPC(RPCOption option, string methodName, params object[] parameters)
		{
			RPC(option, NetworkConfig.DefaultChannel, methodName, parameters);
		}

		[ClientOnly]
		private void RPC_ToServer(QosType channel, string methodName, params object[] parameters)
		{
			// 서버 자신도 ToServer 옵션을 사용하져 자신의 메소드를 호출할 수 있다.
			if (NetworkService.IsServer)
			{
				SendReflectionMessage(null, methodName, parameters);
			}
			else
			{
				var sv = RMPPeer.ServerPeer;
				if (sv != null)
				{
					RPC(sv, channel, methodName, parameters);
				}
				else
				{
					Debug.LogError("Server peer not found. RPC aborted.");
				}
			}
		}
		
		[ServerOnly]
		private void RPC_Broadcast(QosType channel, string methodName, params object[] parameters)
		{
			if (!NetworkService.IsServer)
			{
				Debug.LogError("Only server can use Braodcast option. RPC aborted.");
				return;
			}

			var cls = RMPPeer.ClientPeers;
			if (cls != null)
			{
				foreach (var cl in cls.Values)
				{
					RPC(cl, channel, methodName, parameters);
				}
			}
		}

		/// <summary>
		/// 자기 자신을 복제한다.
		/// </summary>
		/// <param name="to">null 이면 모두에게, 아니면 해당 peer에게만.</param>
		[ServerOnly]
		public void Replicate(RMPPeer to = null)
		{
			RMPNetworkService.Replicate(this, to);
		}
	}
}