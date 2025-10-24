using UnityEngine;
using UnityEngine.UI;

public class TraderButton : MonoBehaviour {

    public Styles styles;
    public Button button;
    public Image traderImage;
    public Image backgroundImage;

    public void Toggle(bool state) {
        traderImage.color = state? Color.white : styles.nonSelectedTraderHeadshotTint;
        backgroundImage.color = state? styles.selectedTraderBackground : styles.nonSelectedTraderBackground;
    }

}
