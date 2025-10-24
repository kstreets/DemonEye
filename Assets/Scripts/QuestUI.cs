using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour {

    public Image traderImage;
    public Button completeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    public void Set(Quest quest) {
        traderImage.sprite = quest.questGiver.traderHeadshot;
        titleText.text = quest.title;
        descText.text = quest.description;
        completeButton.gameObject.SetActive(quest.canCompleteQuestFlag);
    }

}
