using System;

namespace ChickenIngot.Networking
{
	// 특별한 기능은 없다. 단순히 필드나 메소드에 붙여서 가독성을 높이기 위함.
	
	/// <summary>
	/// 리플렉션으로 호출되는 메소드나 값이 변경되는 필드는 디버깅이 어려우므로
	/// 이 어트리뷰트를 붙여서 구분해 놓는것이 좋다.
	/// </summary>
	public class RMPAttribute : Attribute { }

	/// <summary>
	/// 서버에서만 사용될 메소드나 필드
	/// </summary>
	public class ServerOnlyAttribute : Attribute { }

	/// <summary>
	/// 클라이언트에서만 사용될 메소드나 필드
	/// </summary>
	public class ClientOnlyAttribute : Attribute { }

	/// <summary>
	/// 값이 동기화 되는 필드
	/// </summary>
	public class SyncAttribute : Attribute { }

	/// <summary>
	/// 값이 동기화 되지 않는 필드
	/// </summary>
	public class NoSyncAttribute : Attribute { }
}

