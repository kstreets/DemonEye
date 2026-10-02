using UnityEngine;

public partial class Game {
    
    private bool InTutorial => tutorial.stateMachine != null && !tutorial.stateMachine.OnLastState;
    
    private void InitTutorial(GameState gameState) {
        bool compltedTutorial = gameState != null && gameState.tutorialStateIndex == -1;
        if (compltedTutorial) return;
        
        bool justStartedTutorial = gameState == null;
        if (justStartedTutorial) {
            foreach (ItemWithCount itemsWithCount in config.startingTutorialInventory.itemsWithCounts) {
                TryAddItemToInventory(inventories.stash, itemsWithCount.item, itemsWithCount.count);
            }
        }
        
        tutorial.stateMachine = new();
        tutorial.entryState = tutorial.stateMachine.CreateState();
        tutorial.firstCraftingState = tutorial.stateMachine.CreateState();
        tutorial.craftingDemonEyeState = tutorial.stateMachine.CreateState();
        tutorial.equipingDemonEyeState = tutorial.stateMachine.CreateState();
        tutorial.waitingToEnterSlaughterMap = tutorial.stateMachine.CreateState();
    }
    
    private void TutorialOnMapSelectionEnter() {
        if (tutorial.stateMachine.NotPassedThisState(tutorial.waitingToEnterSlaughterMap)) {
            ToggleHideoutPanels(eyeForgePanel.panel, stashPanel.panel);
            eyeForgePanel.toggleButtonGroup.ManualyToggle(eyeForgePanel.forgeToggle);
            eyeForgePanel.toggleButtonGroup.SetTogglesHidden(true);
        }
    }
    
    private void TutorialOnMapSelectionUpdate() {
        OnHideoutStateUpdate();
        playerPanel.panel.gameObject.SetActive(false);
    }
    
}
