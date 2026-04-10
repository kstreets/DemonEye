using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class PageDots : MonoBehaviour {
    
    public Sprite emptyLevelProgressDotSprite;
    public Sprite filledLevelProgressDotSprite;
    public Image[] levelProgressDots;
    
    public void SetPage(int index) {
        foreach (Image dotImage in levelProgressDots) {
            dotImage.sprite = emptyLevelProgressDotSprite;
        }
        levelProgressDots[index].sprite = filledLevelProgressDotSprite;
    }
    
    public void SetPageCount(int count) {
        Assert.IsTrue(count <= levelProgressDots.Length, $"Not enough images to set page count to {count}"); 
        for (int i = 0; i < levelProgressDots.Length; i++) {
            levelProgressDots[i].gameObject.SetActive(i < count);
        }
    }
    
}
