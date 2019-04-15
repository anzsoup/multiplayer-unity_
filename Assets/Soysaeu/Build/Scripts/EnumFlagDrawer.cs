#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public class EnumFlagAttribute : PropertyAttribute
{
	public string enumName;

	public EnumFlagAttribute() { }

	public EnumFlagAttribute(string name)
	{
		enumName = name;
	}
}

/// <summary>
/// EnumFlag Drawer, enums tagged [EnumFlag] now show mask selection instead of singular enum.
/// <para/>
/// Use [SerializeField, EnumFlag] to display the mask selection window instead of a singular enum drop down box in the inspector.
/// This will allow you to select: Nothing, Everything, and multiple of your enum values, to be used as bit masks.
/// </summary>
[CustomPropertyDrawer(typeof(EnumFlagAttribute))]
public class EnumFlagDrawer : PropertyDrawer
{

	/// <summary>
	/// Overrides Unity display of Enums tagged with [EnumFlag].
	/// </summary>
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Grab the enum from the properties target.
		Enum targetEnum = (Enum)fieldInfo.GetValue(property.serializedObject.targetObject);
		EnumFlagAttribute flagSettings = (EnumFlagAttribute)attribute;

		string propName = flagSettings.enumName;
		if (string.IsNullOrEmpty(propName))
			propName = label.text;

		// Start property rendering
		EditorGUI.BeginProperty(position, label, property);
		// Use Unity's Flags field to render the enum instead of the default mask.
		// This allows you to select multiple options.
		Enum enumNew = EditorGUI.EnumFlagsField(position, propName, targetEnum);
		// Convert back to an int value to represent the mask fields, as that is how its stored on the serialized property.
		property.intValue = (int)Convert.ChangeType(enumNew, targetEnum.GetType());
		// Finish property rendering
		EditorGUI.EndProperty();

	}
}

#endif