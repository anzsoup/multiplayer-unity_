using UnityEngine.Networking;

namespace Salgu.Networking
{
	public interface INetworkEventHandler
	{
		/// <summary>
		/// NetworkEventType.ConnectEvent 이벤트가 발생했을 때.
		/// 원격 사용자가 해당 호스트를 통해 접속한 경우.
		/// </summary>
		void OnConnectEvent(int hostId, int connectionId, int channelId);

		/// <summary>
		/// NetworkEventType.DataEvent 이벤트가 발생했을 때.
		/// 원격 사용자로부터 데이터 스트림을 수신한 경우.
		/// </summary>
		/// <param name="buffer">수신한 데이터.</param>
		/// <param name="dataSize">데이터의 크기.</param>
		void OnDataEvent(int hostId, int connectionId, int channelId, byte[] buffer, int dataSize);

		/// <summary>
		/// NetworkEventType.DisconnectEvent 이벤트가 발생했을 때.
		/// 원격 사용자가 접속을 정상적으로 종료한 경우.
		/// </summary>
		void OnDisconnectEvent(int hostId, int connectionId, int channelId);

		/// <summary>
		/// 에러가 발생했을 때.
		/// 에러코드가 NetworkError.Ok 가 아닌 경우.
		/// </summary>
		void OnError(int hostId, int connectionId, int channelId, NetworkError error);

		/// <summary>
		/// 네트워킹을 종료하고자 할 때 호출된다.
		/// </summary>
		void Stop();

		/// <summary>
		/// 모든 작업을 마쳐서 정상적인 종료가 가능한 상태일 때 true 를 반환하면 된다.
		/// true 일 경우 즉시 핸들러는 제거되고 오프라인 상태가 된다.
		/// </summary>
		bool IsDead();

		/// <summary>
		/// IsDead() 가 true 를 반환하여 핸들러가 제거될 때 호출된다.
		/// 호스트를 제거하는 등 리소스를 정리할 때 사용.
		/// </summary>
		void OnRemoved();
	}
}
