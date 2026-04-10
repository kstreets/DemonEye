using UnityEngine;

public class WhatsMyRectSize : MonoBehaviour {
    
    private void Awake() {
        Debug.Log($"My rect size is {GetComponent<RectTransform>().rect.width}");
    }

}
