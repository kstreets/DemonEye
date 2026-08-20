using UnityEditor;
using UnityEngine;
using static Game;

[CustomPropertyDrawer(typeof(DropOrigin))]
public class DropOriginsPropertyDrawer : PropertyDrawer {
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        float itemWidth = position.width * 0.35f;
        float sliderWidth = position.width * 0.55f;
        float maxStackWidth = position.width * 0.1f;
        const float padding = 4f;

        var itemRect = new Rect(position.x, position.y, itemWidth - padding, position.height);
        var weightRect = new Rect(position.x + itemWidth, position.y, sliderWidth - padding, position.height);
        var maxStackRect = new Rect(position.x + itemWidth + sliderWidth, position.y, maxStackWidth, position.height);

        var itemProp = property.FindPropertyRelative(nameof(DropOrigin.dropPool));
        var weightProp = property.FindPropertyRelative(nameof(DropOrigin.chanceToSpawn));
        var maxStackProp = property.FindPropertyRelative(nameof(DropOrigin.maxStackCount)); 

        EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none);
        EditorGUI.Slider(weightRect, weightProp, 0f, 1f, GUIContent.none);
        EditorGUI.PropertyField(maxStackRect, maxStackProp, GUIContent.none);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight;
    }
    
}