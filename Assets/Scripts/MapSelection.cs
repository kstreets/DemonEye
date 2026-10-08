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

    private static readonly int dissolveAmountId = Shader.PropertyToID("_Completion");
    private static readonly int opacityId = Shader.PropertyToID("_Opacity");

    private bool initialized;
    private string unlockedSubName; // The locked state overwrites the sub name, so this is what gets put back once unlocked

    private void Init() {
        if (initialized) return;
        initialized = true;

        unlockedSubName = subNameTextMesh.text;

        burnEffectImage.material = new(burnEffectImage.material);
        burnEffectImage.material.SetFloat(dissolveAmountId, 0f);

        scortchedImage.material = new(scortchedImage.material);
        scortchedImage.material.SetFloat(opacityId, 0f);
        scortchedImage.material.SetFloat(dissolveAmountId, 0f);
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

    private QuestUI.BurnData burnData = new();
    private Tween emberTween;
    private Tween scortchedTween;
    private Tween burningTween;

    // Same burn as leveling up a skill
    public void Burn(float duration, AnimationCurve edgeCurve, AnimationCurve particlesCurve) {
        Init();
        emberTween.Complete();
        scortchedTween.Complete();
        burningTween.Complete();

        burnData.burnEffectImage = burnEffectImage;
        burnData.scortchedImage = scortchedImage;
        burnData.emberSpawner = emberSpawner;
        burnData.edgeCurve = edgeCurve;
        burnData.particleCurve = particlesCurve;

        emberSpawner.Play();
        emberTween = Tween.Delay(emberSpawner, duration * 0.45f, static (emberSpawner) => emberSpawner.Stop());

        scortchedImage.material.SetFloat(opacityId, 1f);
        scortchedTween = Tween.Custom(scortchedImage, 1f, 0f, duration, startDelay: duration * 0.35f, onValueChange: static (scortchedImage, comp) => {
            scortchedImage.material.SetFloat(opacityId, comp);
        })
        .OnComplete(scortchedImage, static (scortchedImage) => scortchedImage.material.SetFloat(opacityId, 0f));

        burningTween = Tween.Custom(burnData, 0f, 1f, duration, onValueChange: static (data, comp) => {
            float burnComp = data.edgeCurve.Evaluate(comp);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, burnComp);
            data.scortchedImage.material.SetFloat(dissolveAmountId, burnComp);
            data.emberSpawner.BurnProgress = data.particleCurve.Evaluate(comp);
        })
        .OnComplete(burnData, static (data) => {
            data.scortchedImage.material.SetFloat(dissolveAmountId, 0f);
            data.burnEffectImage.material.SetFloat(dissolveAmountId, 0f);
        });
    }

}
