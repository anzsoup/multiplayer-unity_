using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace LLenok.Networking
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

		public static void PushParameter(Packet msg, object parameter)
		{
			if (parameter == null) msg.Push((Byte)ParameterType.None);
			else if (parameter is Byte)
			{
				msg.Push((Byte)ParameterType.Byte);
				msg.Push((Byte)parameter);
			}
			else if (parameter is SByte)
			{
				msg.Push((Byte)ParameterType.SByte);
				msg.Push((SByte)parameter);
			}
			else if (parameter is Int16)
			{
				msg.Push((Byte)ParameterType.Int16);
				msg.Push((Int16)parameter);
			}
			else if (parameter is UInt16)
			{
				msg.Push((Byte)ParameterType.UInt16);
				msg.Push((UInt16)parameter);
			}
			else if (parameter is Int32)
			{
				msg.Push((Byte)ParameterType.Int32);
				msg.Push((Int32)parameter);
			}
			else if (parameter is UInt32)
			{
				msg.Push((Byte)ParameterType.UInt32);
				msg.Push((UInt32)parameter);
			}
			else if (parameter is Int64)
			{
				msg.Push((Byte)ParameterType.Int64);
				msg.Push((Int64)parameter);
			}
			else if (parameter is UInt64)
			{
				msg.Push((Byte)ParameterType.UInt64);
				msg.Push((UInt64)parameter);
			}
			else if (parameter is Single)
			{
				msg.Push((Byte)ParameterType.Single);
				msg.Push((Single)parameter);
			}
			else if (parameter is Double)
			{
				msg.Push((Byte)ParameterType.Double);
				msg.Push((Double)parameter);
			}
			else if (parameter is Char)
			{
				msg.Push((Byte)ParameterType.Char);
				msg.Push((Char)parameter);
			}
			else if (parameter is Boolean)
			{
				msg.Push((Byte)ParameterType.Boolean);
				msg.Push((Boolean)parameter);
			}
			else if (parameter is String)
			{
				msg.Push((Byte)ParameterType.String);
				msg.Push((String)parameter);
			}
			else if (parameter is Byte[])
			{
				msg.Push((Byte)ParameterType.ByteArray);
				Byte[] param = (Byte[])parameter;
				msg.Push(param);
			}
			else if (parameter is Vector2)
			{
				msg.Push((Byte)ParameterType.Vector2);
				msg.Push((Vector2)parameter);
			}
			else if (parameter is Vector3)
			{
				msg.Push((Byte)ParameterType.Vector3);
				msg.Push((Vector3)parameter);
			}
			else if (parameter is Quaternion)
			{
				msg.Push((Byte)ParameterType.Quaternion);
				msg.Push((Quaternion)parameter);
			}
			else if (parameter is Vector2Int)
			{
				msg.Push((Byte)ParameterType.Vector2Int);
				msg.Push((Vector2Int)parameter);
			}
			else if (parameter is Vector3Int)
			{
				msg.Push((Byte)ParameterType.Vector3Int);
				msg.Push((Vector3Int)parameter);
			}
		}

		public static object PopParameter(Packet msg)
		{
			ParameterType paramType = (ParameterType)msg.PopByte();
			object parameter = null;
			switch (paramType)
			{
				case ParameterType.None:
					// object[]가 아닌 null 값을 반환해야한다
					parameter = null;
					break;

				case ParameterType.Byte:
					parameter = msg.PopByte();
					break;

				case ParameterType.SByte:
					parameter = msg.PopSByte();
					break;

				case ParameterType.Int16:
					parameter = msg.PopInt16();
					break;

				case ParameterType.UInt16:
					parameter = msg.PopUInt16();
					break;

				case ParameterType.Int32:
					parameter = msg.PopInt32();
					break;

				case ParameterType.UInt32:
					parameter = msg.PopUInt32();
					break;

				case ParameterType.Int64:
					parameter = msg.PopInt64();
					break;

				case ParameterType.UInt64:
					parameter = msg.PopUInt64();
					break;

				case ParameterType.Single:
					parameter = msg.PopSingle();
					break;

				case ParameterType.Double:
					parameter = msg.PopDouble();
					break;

				case ParameterType.Char:
					parameter = msg.PopChar();
					break;

				case ParameterType.Boolean:
					parameter = msg.PopBoolean();
					break;

				case ParameterType.String:
					parameter = msg.PopString();
					break;

				case ParameterType.ByteArray:
					parameter = msg.PopByteArray();
					break;

				case ParameterType.Vector2:
					parameter = msg.PopVector2();
					break;

				case ParameterType.Vector3:
					parameter = msg.PopVector3();
					break;

				case ParameterType.Quaternion:
					parameter = msg.PopQuaternion();
					break;

				case ParameterType.Vector2Int:
					parameter = msg.PopVector2Int();
					break;

				case ParameterType.Vector3Int:
					parameter = msg.PopVector3Int();
					break;
			}

			return parameter;
		}
	}
}