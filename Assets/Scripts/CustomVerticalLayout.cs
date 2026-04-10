using UnityEngine;
using UnityEngine.UI;

public class CustomVerticalLayout : MonoBehaviour, ILayoutGroup, ILayoutElement {
    
    public void SetLayoutHorizontal() {
        throw new System.NotImplementedException();
    }
    
    public void SetLayoutVertical() {
        throw new System.NotImplementedException();
    }
    
    public void CalculateLayoutInputHorizontal() {
        throw new System.NotImplementedException();
    }
    
    public void CalculateLayoutInputVertical() {
        throw new System.NotImplementedException();
    }
    
    public float minWidth { get; }
    public float preferredWidth { get; }
    public float flexibleWidth { get; }
    public float minHeight { get; }
    public float preferredHeight { get; }
    public float flexibleHeight { get; }
    public int layoutPriority { get; }
    
}