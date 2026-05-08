using UnityEditor;
using UnityEngine;
using static Game;

[CustomPropertyDrawer(typeof(DropOrigin))]
public class DropOriginsPropertyDrawer : PropertyDrawer {
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        float itemWidth = position.width * 0.4f;
        float sliderWidth = position.width * 0.6f;
        const float padding = 4f;

        var itemRect = new Rect(position.x, position.y, itemWidth - padding, position.height);
        var weightRect = new Rect(position.x + itemWidth, position.y, sliderWidth, position.height);

        var itemProp = property.FindPropertyRelative(nameof(DropOrigin.dropPool));
        var weightProp = property.FindPropertyRelative(nameof(DropOrigin.chanceToSpawn));

        EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);
        EditorGUI.Slider(weightRect, weightProp, 0f, 1f, GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight;
    }
    
}