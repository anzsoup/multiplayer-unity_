using System;
using UnityEngine;

namespace ChickenIngot.Networking
{
	/// <summary>
	/// 바이트 스트림을 쉽게 읽고 쓰기 위한 인터페이스를 제공한다.
	/// </summary>
	public partial class Packet
	{
		// 세그먼팅 되지 않는 적당한 크기임
		public const int BUFFER_LENGTH = 1440;

		public byte[] Buffer { get; private set; }
		public int Position { get; private set; }
		public int Size { get { return Position; } }

		public Packet()
		{
			Buffer = new byte[BUFFER_LENGTH];
			Position = 0;
		}

		public Packet(byte[] buffer)
		{
			Buffer = new byte[BUFFER_LENGTH];
			Array.Copy(buffer, Buffer, buffer.Length);
			Position = 0;
		}

		public Packet(Packet orig)
		{
			Buffer = new byte[BUFFER_LENGTH];
			Array.Copy(orig.Buffer, Buffer, orig.Buffer.Length);
			Position = orig.Position;
		}

		private void MovePosition(int size)
		{
			Position += size;
			if (Position >= BUFFER_LENGTH)
				UnityEngine.Debug.LogWarning("[Packet] Position overflow. It will cause ArrayOutOfRangeException.");
		}

		#region Push

		public void PushByte(byte data)
		{
			Buffer[Position] = data;
			MovePosition(sizeof(byte));
		}

		public void PushBool(bool data)
		{
			if (data)
				PushByte(1);
			else
				PushByte(0);
		}

		public void PushInt16(Int16 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void PushInt32(Int32 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void PushInt64(Int64 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void PushSingle(float data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void PushDouble(double data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void PushByteArray(byte[] data)
		{
			int len = data.Length;
			PushInt32(len);
			data.CopyTo(Buffer, Position);
			MovePosition(data.Length);
		}

		public void PushString(string data)
		{
			if (data == null) data = "";
			byte[] tempBuffer = System.Text.Encoding.UTF8.GetBytes(data);
			PushByteArray(tempBuffer);
		}

		public void PushTransform(Transform transform)
		{
			/// scale은 hierarcy와 밀접하기 때문에
			/// hierarcy 관계까지 완벽하게 주고받을 게 아니라면 scale은 제외한다.

			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;
			PushVector3(position);
			PushQuaternion(rotation);
		}

		public void PushVector2(Vector2 v)
		{
			PushSingle(v.x);
			PushSingle(v.y);
		}

		public void PushVector3(Vector3 v)
		{
			PushSingle(v.x);
			PushSingle(v.y);
			PushSingle(v.z);
		}

		public void PushQuaternion(Quaternion q)
		{
			PushSingle(q.x);
			PushSingle(q.y);
			PushSingle(q.z);
			PushSingle(q.w);
		}

		public void PushVector2Int(Vector2Int v)
		{
			PushInt32(v.x);
			PushInt32(v.y);
		}

		public void PushVector3Int(Vector3Int v)
		{
			PushInt32(v.x);
			PushInt32(v.y);
			PushInt32(v.z);
		}

		#endregion

		#region Pop

		public byte PopByte()
		{
			var data = Buffer[Position];
			MovePosition(sizeof(byte));
			return data;
		}

		public bool PopBool()
		{
			var data = PopByte();
			return data == 1;
		}

		public Int16 PopInt16()
		{
			var data = BitConverter.ToInt16(Buffer, Position);
			MovePosition(sizeof(Int16));
			return data;
		}

		public Int32 PopInt32()
		{
			var data = BitConverter.ToInt32(Buffer, Position);
			MovePosition(sizeof(Int32));
			return data;
		}

		public Int64 PopInt64()
		{
			var data = BitConverter.ToInt64(Buffer, Position);
			MovePosition(sizeof(Int64));
			return data;
		}

		public float PopSingle()
		{
			var data = BitConverter.ToSingle(Buffer, Position);
			MovePosition(sizeof(float));
			return data;
		}

		public double PopDouble()
		{
			var data = BitConverter.ToDouble(Buffer, Position);
			MovePosition(sizeof(double));
			return data;
		}

		public byte[] PopByteArray()
		{
			int len = PopInt32();
			byte[] data = new byte[len];
			Array.Copy(Buffer, Position, data, 0, len);
			MovePosition(len);
			return data;
		}

		public string PopString()
		{
			int len = PopInt32();
			var data = System.Text.Encoding.UTF8.GetString(Buffer, Position, len);
			MovePosition(len);
			return data;
		}

		public void PopTransform(Transform transform)
		{
			Vector3 position = PopVector3();
			Quaternion rotation = PopQuaternion();
			transform.position = position;
			transform.rotation = rotation;
		}

		public Vector2 PopVector2()
		{
			return new Vector2(PopSingle(), PopSingle());
		}

		public Vector3 PopVector3()
		{
			return new Vector3(PopSingle(), PopSingle(), PopSingle());
		}

		public Quaternion PopQuaternion()
		{
			return new Quaternion(PopSingle(), PopSingle(), PopSingle(), PopSingle());
		}

		public Vector2Int PopVector2Int()
		{
			return new Vector2Int(PopInt32(), PopInt32());
		}

		public Vector3Int PopVector3Int()
		{
			return new Vector3Int(PopInt32(), PopInt32(), PopInt32());
		}

		#endregion
	}
}