using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FetchQuest", menuName = "Scriptable Objects/FetchQuest")]
public class FetchQuest : Quest {

    public List<ItemWithCount> itemsToFetch;

    public override void UpdateQuest(Game game) {
        if (canCompleteQuestFlag) return;
        
        bool haveAllItemsNeeded = true;
        
        foreach (ItemWithCount itemWithCount in itemsToFetch) {
            int count = game.GetItemCountInInventory(game.playerInventory, itemWithCount.item);
            count += game.GetItemCountInInventory(game.stashInventory, itemWithCount.item);
            if (count < itemWithCount.count) {
                haveAllItemsNeeded = false;
                break;
            }
        }
        
        canCompleteQuestFlag = haveAllItemsNeeded;
    }
    
}
