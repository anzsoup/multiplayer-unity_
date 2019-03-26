using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ChickenIngot.Networking.Demo
{
	/// -----------------------------------------------------------------------------------
	/// [Rigidbody 동기화]
	///    UnityNetworkView 컴포넌트의 'Synced Rigidbody' 에 Rigidbody 를 끌어다 놓는다.
	/// -----------------------------------------------------------------------------------

	/// -----------------------------------------------------------------------------------
	/// [Replicate]
	///	   서버의 오브젝트를 클라이언트에 복제하여 서로 RMP 통신이 가능하게 한다.
	///    프리팹이 RMPUnityService 의 'Replicate Table' 에 등록되어 있어야 한다.
	/// -----------------------------------------------------------------------------------

	public class NetworkingDemoCube : MonoBehaviour
	{
		[SerializeField]
		private RMPNetworkView _view;
		[SerializeField]
		private Rigidbody _rigidbody;
		private float _lastSyncTime;

		public RMPNetworkView View { get { return _view; } }

		public static readonly List<NetworkingDemoCube> CUBES = new List<NetworkingDemoCube>();
		public static void Clear()
		{
			foreach (var cube in CUBES)
				if (cube != null)
					Destroy(cube.gameObject);

			CUBES.Clear();
		}

		// Replicate 될 때 자동으로 호출되는 메소드.
		// 서버는 Replicate 시 함께 보낼 데이터를 기록할 수 있음. 서버에서 MessageSender 는 null 이다.
		// 클라이언트는 데이터를 갖고 초기화하는 데 활용할 수 있음.
		[RMP]
		void OnReplicate(Packet msg)
		{
			if (NetworkService.IsServer)
				msg.PushTransform(transform);
			else
				msg.PopTransform(transform);

			CUBES.Add(this);
			_lastSyncTime = Time.time;

			if (NetworkService.IsOnline && NetworkService.IsServer)
				StartCoroutine(RigidbodySyncLoop());
		}

		[ServerOnly]
		private IEnumerator RigidbodySyncLoop()
		{
			// OnReplicate 에서 코루틴이 시작되면 Replicate 패킷보다 RPC 패킷이 먼저 전송되므로
			// 그 문제를 회피하기 위해 실행을 조금 늦춘다.
			yield return null;

			while (NetworkService.IsOnline)
			{
				_view.RPC(RPCOption.Broadcast, "clRPC_SyncRigidbody", 
					_rigidbody.position, _rigidbody.velocity, _rigidbody.rotation, _rigidbody.angularVelocity);

				yield return new WaitForSeconds(1f / 20f);
			}
		}

		[RMP]
		[ClientOnly]
		private void clRPC_SyncRigidbody(Vector3 pos, Vector3 vel, Quaternion rot, Vector3 angvel)
		{
			float syncDelay = Time.time - _lastSyncTime;
			_lastSyncTime = Time.time;
			InterpolateRigidbody(pos, vel, rot, angvel, syncDelay);
		}

		private void InterpolateRigidbody(Vector3 position, Vector3 linearVelocity, Quaternion rotation, Vector3 angularVelocity, float syncDelay)
		{
			Vector3 syncEndPosition = position;
			Quaternion syncEndRotation = rotation;
			_rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, syncEndPosition, Time.deltaTime / syncDelay));
			_rigidbody.velocity = linearVelocity;
			_rigidbody.MoveRotation(Quaternion.Lerp(_rigidbody.rotation, syncEndRotation, Time.deltaTime / syncDelay));
			_rigidbody.angularVelocity = angularVelocity;
		}
	}
}