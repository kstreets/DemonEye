using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour {

    public RectTransform rectTransform;
    public Image image;
    public TextMeshProUGUI countText;
    
    private void Awake() {
        randomSeed = new(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue));
    }

    private void Update() {
        tweenScaleTimer.Tick();
    }

    public void SetItem(Item data, int count) {
        image.sprite = data.inventorySprite;
        image.enabled = true;
        countText.text = count.ToString();
    }

    public void UpdateCount(int count) {
        countText.text = count.ToString();
    }
    
    public void ClearItem() {
        image.sprite = null;
        image.enabled = false;
        countText.text = "";
    }
    
    private Vector2 randomSeed;
    private float perlinPos;

    public void Shake(float jitter, float magnitude) {
        perlinPos = (perlinPos + jitter * Time.deltaTime) % 1f;
        float x = (Mathf.PerlinNoise(randomSeed.x, perlinPos) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(randomSeed.y, perlinPos + 100f) - 0.5f) * 2f;
        Vector3 targetVector = new Vector3(x, y, rectTransform.position.z) * magnitude;
        rectTransform.anchoredPosition = targetVector; 
    }

    
    private Timer tweenScaleTimer;
    
    public void TweenScale(float startSize, float endSize, float time, Tween.Curve curve) {
        tweenScaleTimer.SetTime(time);
        tweenScaleTimer.UpdateAction = () => {
            float comp = Tween.ConvertCompletion(tweenScaleTimer.Comp(), curve);
            float size = Mathf.Lerp(startSize, endSize, comp);
            rectTransform.localScale = new(size, size, size);
        };
    }
    
}
