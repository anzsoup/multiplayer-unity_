using System;
using System.IO;
using UnityEngine;

namespace Salgu.Networking
{
	/// <summary>
	/// 바이트 스트림을 쉽게 읽고 쓰기 위한 인터페이스를 제공한다.
	/// </summary>
	public sealed partial class Packet
	{
		// 세그먼팅 되지 않는 적당한 크기임
		public const int DEFAULT_SIZE = 1440;

		/// <summary>
		/// To avoid memory allocation when Read() called.
		/// </summary>
		byte[] _readBuffer = new byte[DEFAULT_SIZE];

		/// <summary>
		/// You are welcome to manage buffer directly if you want!
		/// </summary>
		public MemoryStream Buffer { get; private set; }

		/// <summary>
		/// Equal to Buffer.Length
		/// </summary>
		public long Length { get { return Buffer.Length; } }

		public Packet()
		{
			Buffer = new MemoryStream(DEFAULT_SIZE);
		}

		public Packet(int capacity)
		{
			Buffer = new MemoryStream(capacity);
		}

		public Packet(byte[] buffer)
		{
			Buffer = new MemoryStream(buffer);
		}

		public Packet(Packet orig)
		{
			Buffer = new MemoryStream(orig.Buffer.ToArray());
		}

		~Packet()
		{
			Buffer.Dispose();
		}

		public byte[] ToArray()
		{
			return Buffer.ToArray();
		}

		#region Write

		public void Write(Byte data)
		{
			Buffer.WriteByte(data);
		}

		public void Write(SByte data)
		{
			Write((Byte)data);
		}

		public void Write(Boolean data)
		{
			if (data)
				Write((Byte)1);
			else
				Write((Byte)0);
		}

		public void Write(Int16 data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(UInt16 data)
		{
			Write((Int16)data);
		}

		public void Write(Int32 data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(UInt32 data)
		{
			Write((Int32)data);
		}

		public void Write(Int64 data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(UInt64 data)
		{
			Write((Int64)data);
		}

		public void Write(Single data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(Double data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(Char data)
		{
			var temp = BitConverter.GetBytes(data);
			Buffer.Write(temp, 0, temp.Length);
		}

		public void Write(Byte[] data)
		{
			Write(data.Length);
			Buffer.Write(data, 0, data.Length);
		}

		public void Write(String data)
		{
			if (data == null) data = "";
			var temp = System.Text.Encoding.UTF8.GetBytes(data);
			Write(temp);
		}

		public void Write(Transform transform)
		{
			/// scale은 hierarcy와 밀접하기 때문에
			/// hierarcy 관계까지 완벽하게 주고받을 게 아니라면 scale은 제외한다.

			var position = transform.position;
			var rotation = transform.rotation;
			Write(position);
			Write(rotation);
		}

		public void Write(Vector2 v)
		{
			Write(v.x);
			Write(v.y);
		}

		public void Write(Vector3 v)
		{
			Write(v.x);
			Write(v.y);
			Write(v.z);
		}

		public void Write(Quaternion q)
		{
			Write(q.x);
			Write(q.y);
			Write(q.z);
			Write(q.w);
		}

		public void Write(Vector2Int v)
		{
			Write(v.x);
			Write(v.y);
		}

		public void Write(Vector3Int v)
		{
			Write(v.x);
			Write(v.y);
			Write(v.z);
		}

		#endregion

		#region Read

		public Byte ReadByte()
		{
			return (Byte)Buffer.ReadByte();
		}

		public SByte ReadSByte()
		{
			return (SByte)ReadByte();
		}

		public Int16 ReadInt16()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Int16));
			return BitConverter.ToInt16(_readBuffer, 0);
		}

		public UInt16 ReadUInt16()
		{
			var data = (UInt16)ReadInt16();
			return data;
		}

		public Int32 ReadInt32()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Int32));
			return BitConverter.ToInt32(_readBuffer, 0);
		}

		public UInt32 ReadUInt32()
		{
			var data = (UInt32)ReadInt32();
			return data;
		}

		public Int64 ReadInt64()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Int64));
			return BitConverter.ToInt64(_readBuffer, 0);
		}

		public UInt64 ReadUInt64()
		{
			var data = (UInt64)ReadInt64();
			return data;
		}

		public Single ReadSingle()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Single));
			return BitConverter.ToSingle(_readBuffer, 0);
		}

		public Double ReadDouble()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Double));
			return BitConverter.ToDouble(_readBuffer, 0);
		}

		public Char ReadChar()
		{
			Buffer.Read(_readBuffer, 0, sizeof(Char));
			return BitConverter.ToChar(_readBuffer, 0);
		}

		public Boolean ReadBoolean()
		{
			var data = ReadByte();
			return data == 1;
		}

		public String ReadString()
		{
			var len = ReadInt32();
			Buffer.Read(_readBuffer, 0, len);
			var data = System.Text.Encoding.UTF8.GetString(_readBuffer, 0, len);
			return data;
		}

		public Byte[] ReadByteArray()
		{
			var len = ReadInt32();
			var data = new Byte[len];
			Buffer.Read(data, 0, len);
			return data;
		}

		public void ReadTransform(Transform transform)
		{
			var position = ReadVector3();
			var rotation = ReadQuaternion();
			transform.position = position;
			transform.rotation = rotation;
		}

		public Vector2 ReadVector2()
		{
			return new Vector2(ReadSingle(), ReadSingle());
		}

		public Vector3 ReadVector3()
		{
			return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
		}

		public Quaternion ReadQuaternion()
		{
			return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
		}

		public Vector2Int ReadVector2Int()
		{
			return new Vector2Int(ReadInt32(), ReadInt32());
		}

		public Vector3Int ReadVector3Int()
		{
			return new Vector3Int(ReadInt32(), ReadInt32(), ReadInt32());
		}

		#endregion
	}
}