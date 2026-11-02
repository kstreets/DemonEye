using UnityEditor;
using UnityEngine;
using static Game;

[CustomPropertyDrawer(typeof(Game.DropOrigin))]
public class DropOriginsPropertyDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        float itemWidth = position.width * 0.85f;
        float maxStackWidth = position.width * 0.15f;
        const float padding = 4f;

        var itemRect = new Rect(position.x, position.y, itemWidth - padding, position.height);
        var maxStackRect = new Rect(position.x + itemWidth, position.y, maxStackWidth, position.height);

        var itemProp = property.FindPropertyRelative(nameof(Game.DropOrigin.dropPool));
        var maxStackProp = property.FindPropertyRelative(nameof(Game.DropOrigin.maxStackCount));

        EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);
        EditorGUI.PropertyField(maxStackRect, maxStackProp, GUIContent.none);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight;
    }

}
