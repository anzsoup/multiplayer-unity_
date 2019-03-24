using UnityEngine;
using System;
using UnityEditor;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
	// Necessary since some properties tend to collapse smaller than their content
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	// Draw a disabled property field
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		GUI.enabled = !Application.isPlaying && ((ReadOnlyAttribute)attribute).runtimeOnly;
		EditorGUI.PropertyField(position, property, label, true);
		GUI.enabled = true;
	}
}
#endif

[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute
{
	public readonly bool runtimeOnly;

	public ReadOnlyAttribute(bool runtimeOnly = false)
	{
		this.runtimeOnly = runtimeOnly;
	}
}