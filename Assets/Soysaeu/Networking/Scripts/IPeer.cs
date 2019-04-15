
namespace Soysaeu.Networking
{
	/// <summary>
	/// 원격 사용자와의 프로토콜을 정의한다.
	/// </summary>
	public interface IPeer
	{
		/// <summary>
		/// 원격 사용자가 접속했을 때.
		/// </summary>
		/// <param name="hostId">접속을 받은 호스트의 id</param>
		/// <param name="connectionId">해당 세션의 id</param>
		void OnCreated(int hostId, int connectionId);

		/// <summary>
		/// 원격 사용자가 접속을 종료했을 때.
		/// </summary>
		void OnRemoved();

		/// <summary>
		/// 원격 사용자로부터 패킷을 수신했을 때.
		/// </summary>
		/// <param name="msg">수신한 패킷</param>
		void OnMessage(Packet msg);
	}
}
