using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelection : MonoBehaviour {

    public Image icon;
    public TextMeshProUGUI mapNameTextMesh;
    public TextMeshProUGUI subNameTextMesh;
    public GameObject iconFrame;
    public Sprite lockedIcon;
    public ButtonFeel selectionButton;

    [Header("Unlock Burn")]
    public Image burnEffectImage;
    public Image scortchedImage;
    public BurnEdgeEmberSpawner emberSpawner;

    private bool initialized;
    private string unlockedSubName; // The locked state overwrites the sub name, so this is what gets put back once unlocked

    private void Init() {
        if (initialized) return;
        initialized = true;

        unlockedSubName = subNameTextMesh.text;
        burnEffect = new(burnEffectImage, emberSpawner, scortchedImage, emberStopPoint: 0.45f);
    }

    // showAsLocked keeps an unlocked map looking locked until its unlock burn plays
    public void SetState(MapData map, bool showAsLocked = false, bool fadeIn = false) {
        Init();
        mapNameTextMesh.text = map.displayName;

        bool unlocked = map.state.isUnlocked && !showAsLocked;
        selectionButton.gameObject.SetActive(unlocked);
        selectionButton.SetClickableState(unlocked);

        if (unlocked) {
            icon.sprite = map.icon;
            iconFrame.SetActive(true);
            subNameTextMesh.text = unlockedSubName;
        }
        else {
            icon.sprite = lockedIcon;
            iconFrame.SetActive(false);
            subNameTextMesh.text = map.unlockRequirement;
        }

        if (fadeIn) {
            FadeInContents();
        }
    }

    private Sequence contentsFade;

    // Fades in everything but the background, so the burn effects on top don't fade along with it
    private void FadeInContents() {
        const float fadeTime = 0.5f;
        contentsFade.Complete();
        contentsFade = Sequence.Create()
            .Group(FadeInObject(icon.gameObject, fadeTime))
            .Group(FadeInObject(mapNameTextMesh.gameObject, fadeTime))
            .Group(FadeInObject(subNameTextMesh.gameObject, fadeTime))
            .Group(FadeInObject(selectionButton.gameObject, fadeTime));
    }

    private static Tween FadeInObject(GameObject target, float time) {
        if (!target.TryGetComponent(out CanvasGroup canvasGroup)) {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        return Tween.Alpha(canvasGroup, 0f, 1f, time, Ease.OutQuad);
    }

    private UIBurnEffect burnEffect;

    // Same burn as leveling up a skill
    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particlesCurve) {
        Init();
        burnEffect.Burn(duration, edgeCurve, particlesCurve);
    }

}
