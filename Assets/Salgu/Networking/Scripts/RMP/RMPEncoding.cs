using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Salgu.Networking
{
	// 미완성
	[AttributeUsage(AttributeTargets.Method)]
	public class RMPEncodingRuleAttribute : Attribute
	{
		public Type Type { get; private set; }

		public RMPEncodingRuleAttribute(Type type)
		{
			Type = type;
		}
	}

	public static class RMPEncoding
	{
		public enum ProtocolId : byte
		{
			RPC,
			Replicate,
			Remove,
		}

		public enum ParameterType : byte
		{
			None,
			Byte,
			SByte,
			Int16,
			UInt16,
			Int32,
			UInt32,
			Int64,
			UInt64,
			Single,
			Double,
			Char,
			Boolean,
			String,
			DateTime,
			ByteArray,
			Vector2,
			Vector3,
			Quaternion,
			Vector2Int,
			Vector3Int,
		}

		private static Dictionary<string, Action> _rules;

		public static void RegisterEncodingRules()
		{
			_rules = new Dictionary<string, Action>();
			var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			
			var types = Assembly.GetExecutingAssembly().GetTypes();
			foreach (var type in types)
			{
				var methods = type.GetMethods(flags);
				foreach (var method in methods)
				{
					var attribute = Attribute.GetCustomAttribute(
						method, typeof(RMPEncodingRuleAttribute)) as RMPEncodingRuleAttribute;

					if (attribute == null)
						continue;

					var key = attribute.Type.ToString();
					if (_rules.ContainsKey(key))
					{
						Debug.LogWarning(string.Format("Multiple definition of RMP encoding rule. : {0}", key));
						continue;
					}

					var proc = Delegate.CreateDelegate(typeof(Action), method) as Action;
					_rules.Add(key, proc);
				}
			}
		}

		public static void WriteParameter(Packet msg, object parameter)
		{
			if (parameter == null) msg.Write((Byte)ParameterType.None);
			else if (parameter is Byte)
			{
				msg.Write((Byte)ParameterType.Byte);
				msg.Write((Byte)parameter);
			}
			else if (parameter is SByte)
			{
				msg.Write((Byte)ParameterType.SByte);
				msg.Write((SByte)parameter);
			}
			else if (parameter is Int16)
			{
				msg.Write((Byte)ParameterType.Int16);
				msg.Write((Int16)parameter);
			}
			else if (parameter is UInt16)
			{
				msg.Write((Byte)ParameterType.UInt16);
				msg.Write((UInt16)parameter);
			}
			else if (parameter is Int32)
			{
				msg.Write((Byte)ParameterType.Int32);
				msg.Write((Int32)parameter);
			}
			else if (parameter is UInt32)
			{
				msg.Write((Byte)ParameterType.UInt32);
				msg.Write((UInt32)parameter);
			}
			else if (parameter is Int64)
			{
				msg.Write((Byte)ParameterType.Int64);
				msg.Write((Int64)parameter);
			}
			else if (parameter is UInt64)
			{
				msg.Write((Byte)ParameterType.UInt64);
				msg.Write((UInt64)parameter);
			}
			else if (parameter is Single)
			{
				msg.Write((Byte)ParameterType.Single);
				msg.Write((Single)parameter);
			}
			else if (parameter is Double)
			{
				msg.Write((Byte)ParameterType.Double);
				msg.Write((Double)parameter);
			}
			else if (parameter is Char)
			{
				msg.Write((Byte)ParameterType.Char);
				msg.Write((Char)parameter);
			}
			else if (parameter is Boolean)
			{
				msg.Write((Byte)ParameterType.Boolean);
				msg.Write((Boolean)parameter);
			}
			else if (parameter is String)
			{
				msg.Write((Byte)ParameterType.String);
				msg.Write((String)parameter);
			}
			else if (parameter is Byte[])
			{
				msg.Write((Byte)ParameterType.ByteArray);
				Byte[] param = (Byte[])parameter;
				msg.Write(param);
			}
			else if (parameter is Vector2)
			{
				msg.Write((Byte)ParameterType.Vector2);
				msg.Write((Vector2)parameter);
			}
			else if (parameter is Vector3)
			{
				msg.Write((Byte)ParameterType.Vector3);
				msg.Write((Vector3)parameter);
			}
			else if (parameter is Quaternion)
			{
				msg.Write((Byte)ParameterType.Quaternion);
				msg.Write((Quaternion)parameter);
			}
			else if (parameter is Vector2Int)
			{
				msg.Write((Byte)ParameterType.Vector2Int);
				msg.Write((Vector2Int)parameter);
			}
			else if (parameter is Vector3Int)
			{
				msg.Write((Byte)ParameterType.Vector3Int);
				msg.Write((Vector3Int)parameter);
			}
		}

		public static object ReadParameter(Packet msg)
		{
			ParameterType paramType = (ParameterType)msg.ReadByte();
			object parameter = null;
			switch (paramType)
			{
				case ParameterType.None:
					// object[]가 아닌 null 값을 반환해야한다
					parameter = null;
					break;

				case ParameterType.Byte:
					parameter = msg.ReadByte();
					break;

				case ParameterType.SByte:
					parameter = msg.ReadSByte();
					break;

				case ParameterType.Int16:
					parameter = msg.ReadInt16();
					break;

				case ParameterType.UInt16:
					parameter = msg.ReadUInt16();
					break;

				case ParameterType.Int32:
					parameter = msg.ReadInt32();
					break;

				case ParameterType.UInt32:
					parameter = msg.ReadUInt32();
					break;

				case ParameterType.Int64:
					parameter = msg.ReadInt64();
					break;

				case ParameterType.UInt64:
					parameter = msg.ReadUInt64();
					break;

				case ParameterType.Single:
					parameter = msg.ReadSingle();
					break;

				case ParameterType.Double:
					parameter = msg.ReadDouble();
					break;

				case ParameterType.Char:
					parameter = msg.ReadChar();
					break;

				case ParameterType.Boolean:
					parameter = msg.ReadBoolean();
					break;

				case ParameterType.String:
					parameter = msg.ReadString();
					break;

				case ParameterType.ByteArray:
					parameter = msg.ReadByteArray();
					break;

				case ParameterType.Vector2:
					parameter = msg.ReadVector2();
					break;

				case ParameterType.Vector3:
					parameter = msg.ReadVector3();
					break;

				case ParameterType.Quaternion:
					parameter = msg.ReadQuaternion();
					break;

				case ParameterType.Vector2Int:
					parameter = msg.ReadVector2Int();
					break;

				case ParameterType.Vector3Int:
					parameter = msg.ReadVector3Int();
					break;
			}

			return parameter;
		}
	}
}