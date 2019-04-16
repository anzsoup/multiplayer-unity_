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
				Debug.LogWarning("[Packet] Position overflow. It will cause ArrayOutOfRangeException.");
		}

		#region Push

		public void Push(Byte data)
		{
			Buffer[Position] = data;
			MovePosition(sizeof(Byte));
		}

		public void Push(SByte data)
		{
			Push((Byte)data);
		}

		public void Push(Boolean data)
		{
			if (data)
				Push((Byte)1);
			else
				Push((Byte)0);
		}

		public void Push(Int16 data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(UInt16 data)
		{
			Push((Int16)data);
		}

		public void Push(Int32 data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(UInt32 data)
		{
			Push((Int32)data);
		}

		public void Push(Int64 data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(UInt64 data)
		{
			Push((Int64)data);
		}

		public void Push(Single data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(Double data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(Char data)
		{
			var tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(Buffer, Position);
			MovePosition(tempBuffer.Length);
		}

		public void Push(Byte[] data)
		{
			var len = data.Length;
			Push(len);
			data.CopyTo(Buffer, Position);
			MovePosition(data.Length);
		}

		public void Push(String data)
		{
			if (data == null) data = "";
			var tempBuffer = System.Text.Encoding.UTF8.GetBytes(data);
			Push(tempBuffer);
		}

		public void Push(Transform transform)
		{
			/// scale은 hierarcy와 밀접하기 때문에
			/// hierarcy 관계까지 완벽하게 주고받을 게 아니라면 scale은 제외한다.

			var position = transform.position;
			var rotation = transform.rotation;
			Push(position);
			Push(rotation);
		}

		public void Push(Vector2 v)
		{
			Push(v.x);
			Push(v.y);
		}

		public void Push(Vector3 v)
		{
			Push(v.x);
			Push(v.y);
			Push(v.z);
		}

		public void Push(Quaternion q)
		{
			Push(q.x);
			Push(q.y);
			Push(q.z);
			Push(q.w);
		}

		public void Push(Vector2Int v)
		{
			Push(v.x);
			Push(v.y);
		}

		public void Push(Vector3Int v)
		{
			Push(v.x);
			Push(v.y);
			Push(v.z);
		}

		#endregion

		#region Pop

		public Byte PopByte()
		{
			var data = Buffer[Position];
			MovePosition(sizeof(Byte));
			return data;
		}

		public SByte PopSByte()
		{
			var data = (SByte)PopByte();
			return data;
		}

		public Int16 PopInt16()
		{
			var data = BitConverter.ToInt16(Buffer, Position);
			MovePosition(sizeof(Int16));
			return data;
		}

		public UInt16 PopUInt16()
		{
			var data = (UInt16)PopInt16();
			return data;
		}

		public Int32 PopInt32()
		{
			var data = BitConverter.ToInt32(Buffer, Position);
			MovePosition(sizeof(Int32));
			return data;
		}

		public UInt32 PopUInt32()
		{
			var data = (UInt32)PopInt32();
			return data;
		}

		public Int64 PopInt64()
		{
			var data = BitConverter.ToInt64(Buffer, Position);
			MovePosition(sizeof(Int64));
			return data;
		}

		public UInt64 PopUInt64()
		{
			var data = (UInt64)PopInt64();
			return data;
		}

		public Single PopSingle()
		{
			var data = BitConverter.ToSingle(Buffer, Position);
			MovePosition(sizeof(Single));
			return data;
		}

		public Double PopDouble()
		{
			var data = BitConverter.ToDouble(Buffer, Position);
			MovePosition(sizeof(Double));
			return data;
		}

		public Char PopChar()
		{
			var data = BitConverter.ToChar(Buffer, Position);
			MovePosition(sizeof(Char));
			return data;
		}

		public Boolean PopBoolean()
		{
			var data = PopByte();
			return data == 1;
		}

		public String PopString()
		{
			var len = PopInt32();
			var data = System.Text.Encoding.UTF8.GetString(Buffer, Position, len);
			MovePosition(len);
			return data;
		}

		public Byte[] PopByteArray()
		{
			var len = PopInt32();
			var data = new Byte[len];
			Array.Copy(Buffer, Position, data, 0, len);
			MovePosition(len);
			return data;
		}

		public void PopTransform(Transform transform)
		{
			var position = PopVector3();
			var rotation = PopQuaternion();
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