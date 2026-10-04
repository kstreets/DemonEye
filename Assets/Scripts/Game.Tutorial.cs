using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class Game {

    private bool InTutorial => tutorial.stateMachine != null && !tutorial.stateMachine.OnLastState;
    private bool InTutorialFirstForge => InTutorial && tutorial.stateMachine.NotPassedThisState(tutorial.waitingToEnterSlaughterMap);
    private bool InTutorialSlaughterMap => !InTutorialFirstForge && tutorial.stateMachine.NotPassedThisState(tutorial.diedInSlaughterMap);
    private bool InTutorialFirstTraderMeeting => !InTutorialSlaughterMap && tutorial.stateMachine.NotPassedThisState(tutorial.diedInSlaughterMap);

    private void InitTutorial(GameState gameState) {
        bool compltedTutorial = gameState != null && gameState.tutorialStateIndex == -1;
        if (compltedTutorial) return;
        
        int restoreHideoutInputPadding = inputPrompts.hideoutParent.GetComponent<HorizontalLayoutGroup>().padding.left;
        
        tutorial.stateMachine = new();
        tutorial.entryState = tutorial.stateMachine.CreateState();
        tutorial.openingDialogue = tutorial.stateMachine.CreateState(enter: () => {
            StartDialogue( 
                fadeOutTime: 1f,
                Line("Among the demons of Hell dwells the Sinoculus, a solitary one-eyed creature driven toward ascension.", unskippable: true),
                Line("They are known for crafting Demon Eyes, replacements for eyes of their own designed to kill.", unskippable: true),
                Line("Fused with the power of the right blood runes, they become unstoppable.", unskippable: true),
                Line("Even so, many Sinoculus have fallen on the path to ascension, but another journey always awaits.", unskippable: true)
            );
        });
        tutorial.firstCraftingState = tutorial.stateMachine.CreateState(enter: () => {
            eyeForgePanel.panel.gameObject.SetActive(true);
            
            const float fadeInPentagramTime = 1.5f;
            FadeInHideout(riseDistance: 60f, time: fadeInPentagramTime);
            
            const float stashFadeInTime = 1.2f;
            Tween.Delay(fadeInPentagramTime, () => FadeInAndExpandPanel(stashPanel.panel, 1f, stashFadeInTime));
            
            // If we already gave starting items we don't give them again. This is incase the player exits the game and loads back into the tutorial
            if (GetInventoryItemCount(inventories.stash) > 0 || GetInventoryItemCount(inventories.eyeForge) > 0 || inventories.player.slots[0].itemInstance != null) return;
            
            const float startingItemPopInDelay = fadeInPentagramTime + stashFadeInTime + 0.25f;
            const float firstItemGap = 0.2f;
            const float lastItemGap = 0.05f;
            int startingItemCount = config.startingTutorialInventory.itemsWithCounts.Count;
            float itemDelay = startingItemPopInDelay;

            for (int i = 0; i < startingItemCount; i++) {
                ItemWithCount itemsWithCount = config.startingTutorialInventory.itemsWithCounts[i];
                Tween.Delay(itemDelay, () => TryAddItemToInventory(inventories.stash, itemsWithCount.item, itemsWithCount.count));
                float comp = startingItemCount > 1 ? i / (float)(startingItemCount - 1) : 0f;
                itemDelay += Mathf.Lerp(firstItemGap, lastItemGap, comp);
            }
            
            Tween.Delay(itemDelay, () => {
                inputPrompts.hideoutParent.gameObject.GetComponent<HorizontalLayoutGroup>().padding.left = 50;
                inputPrompts.hideoutParent.gameObject.SetActive(true);
            });
        });
        tutorial.craftingDemonEyeState = tutorial.stateMachine.CreateState(enter: () => {
            inventories.stash.isLocked = true;
            inventories.eyeForge.isLocked = true;
            FadeOutAndCollapsePanel(stashPanel.panel, 1f, 1.2f);
        });
        tutorial.equipingDemonEyeState = tutorial.stateMachine.CreateState(enter: () => {
            ToggleSlimPlayerPanel(false);
            FadeInAndExpandPanel(playerPanel.panel, 1f, 1.2f)
            .OnComplete(() => inventories.eyeForge.isLocked = false);
        });
        tutorial.waitingToEnterSlaughterMap = tutorial.stateMachine.CreateState(enter: () => {
            inventories.player.isLocked = true;
            
            FadeOutAndCollapsePanel(eyeForgePanel.panel, 1f, 1.2f);
            
            TextMeshProUGUI headerText = ui.teleportingIntoRaidHeader;
            string origHeaderText = headerText.text;
            headerText.gameObject.gameObject.SetActive(true);

            const int countdownSeconds = 5;
            Sequence countdown = Sequence.Create();
            for (int secondsLeft = countdownSeconds; secondsLeft > 0; secondsLeft--) {
                string text = $"{origHeaderText} in {secondsLeft}";
                countdown.ChainCallback(() => headerText.text = text);
                countdown.Chain(Tween.Scale(headerText.rectTransform, 1.1f, 1f, 1.2f, Ease.OutQuad));
            }
            countdown.ChainCallback(() => {
                // Map selection uses the same header
                headerText.text = origHeaderText;
                LoadMapAsync(config.maps[0]);
                SaveGameState();
            });
        });
        
        tutorial.inSlaughterMap = tutorial.stateMachine.CreateState(enter: () => {
            inventories.player.isLocked = false;
            inventories.stash.isLocked = false;
            inventories.eyeForge.isLocked = false;
            inputPrompts.hideoutParent.gameObject.GetComponent<HorizontalLayoutGroup>().padding.left = restoreHideoutInputPadding;
        });
        tutorial.diedInSlaughterMap = tutorial.stateMachine.CreateState();
        tutorial.firstTraderMeeting = tutorial.stateMachine.CreateState(enter: () => {
            mainMenuSequence.isPaused = true;
            states.gameStateMachine.Pause();
            StartDialogue(
                Line("Well well... you crawled back out of there."),
                Line("Most don't make it back at all."),
                Line("Come, let's see what you can afford.")
            );
        }, exit: () => {
            mainMenuSequence.isPaused = false;
            states.gameStateMachine.UnPause();
        });
        tutorial.completed = tutorial.stateMachine.CreateState();
        
        tutorial.entryState.To(tutorial.openingDialogue).When(() => InHideout);
        tutorial.openingDialogue.To(tutorial.firstCraftingState).When(() => tutorial.dialogue.Finished);
        tutorial.firstCraftingState.To(tutorial.craftingDemonEyeState).When(() => PlayingForgeAnimation);
        tutorial.craftingDemonEyeState.To(tutorial.equipingDemonEyeState).When(() => !PlayingForgeAnimation).WithDelay(0.5f);
        tutorial.equipingDemonEyeState.To(tutorial.waitingToEnterSlaughterMap).When(() => inventories.player.slots[0].itemInstance?.isDemonEye ?? false);
        tutorial.waitingToEnterSlaughterMap.To(tutorial.inSlaughterMap).When(() => curRaid.mapLoadingState is MapLoadingState.Loading or MapLoadingState.Loaded);
        tutorial.inSlaughterMap.To(tutorial.diedInSlaughterMap).When(() => player.health <= 0f);
        tutorial.diedInSlaughterMap.To(tutorial.firstTraderMeeting).When(() => ShowingMainMenu);
        tutorial.firstTraderMeeting.To(tutorial.completed).When(() => tutorial.dialogue.Finished);

        bool restoringTutorialFromSaveIndex = gameState != null;
        if (restoringTutorialFromSaveIndex) {
            State lastSaveState = tutorial.stateMachine.StateFromIndex(gameState.tutorialStateIndex);
            tutorial.stateMachine.SetStateWithoutCallbacks(lastSaveState);
            
            if (InTutorialFirstForge) {
                ClearInventory(inventories.stash);
                ClearInventory(inventories.eyeForge);
                ClearInventory(inventories.player);
                tutorial.stateMachine.SetState(tutorial.entryState);
            }
            
            if (InTutorialSlaughterMap) {
                player.health = FullPlayerHealth();
            }
        }
    }

    private void UpdateTutorial() {
        if (!InTutorial) return;
        UpdateDialogue();
        tutorial.stateMachine.Tick();
    }
    
    private void TutorialOnHideoutEnter() {
        if (!InTutorialFirstForge) return;
        hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.eyeForgeButton);
        eyeForgePanel.toggleButtonGroup.ManualyToggle(eyeForgePanel.forgeToggle);
        playerPanel.panel.gameObject.SetActive(false);
        stashPanel.panel.gameObject.SetActive(false);
        eyeForgePanel.panel.gameObject.SetActive(false);
        ui.menuBackButton.gameObject.SetActive(false);
        playerInfo.parent.gameObject.SetActive(false);
        inputPrompts.hideoutParent.gameObject.SetActive(false);
        hideoutTabs.toggleGroup.SetTogglesHidden(true);
        eyeForgePanel.toggleButtonGroup.SetTogglesHidden(true);
    }

    // ************************
    // Dialogue
    // ************************
    
    public struct DialogueLine {
        public string text;
        public bool unskippable;
    }

    public class Dialogue {
        public DialogueLine[] lines;
        public int lineIndex;
        public float fadeOutTime;
        public Tween fadeOutTween;
        public bool Finished => lines == null;
        public bool FadingOut => fadeOutTween.isAlive;
    }

    private static DialogueLine Line(string text, bool unskippable = false) => new() { text = text, unskippable = unskippable };

    private void StartDialogue(params DialogueLine[] lines) => StartDialogue(fadeOutTime: 0f, lines);

    private void StartDialogue(float fadeOutTime, params DialogueLine[] lines) {
        Dialogue dialogue = tutorial.dialogue;
        dialogue.lines = lines;
        dialogue.lineIndex = -1;
        dialogue.fadeOutTime = fadeOutTime;
        dialogue.fadeOutTween.Stop();
        DialogueCanvasGroup().alpha = 1f;
        ui.dialogueTypewriter.gameObject.SetActive(true);
        ShowNextDialogueLine();
    }

    private CanvasGroup DialogueCanvasGroup() {
        GameObject typewriterObject = ui.dialogueTypewriter.gameObject;
        if (!typewriterObject.TryGetComponent(out CanvasGroup canvasGroup)) {
            canvasGroup = typewriterObject.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    private void ShowNextDialogueLine() {
        Dialogue dialogue = tutorial.dialogue;
        dialogue.lineIndex++;

        bool finishedLastLine = dialogue.lineIndex >= dialogue.lines.Length;
        if (finishedLastLine) {
            if (dialogue.fadeOutTime > 0f) {
                dialogue.fadeOutTween = Tween.Alpha(DialogueCanvasGroup(), 0f, dialogue.fadeOutTime, Ease.OutQuad).OnComplete(EndDialogue);
            } else {
                EndDialogue();
            }
            return;
        }
        ui.dialogueTypewriter.ShowText(dialogue.lines[dialogue.lineIndex].text);
    }

    private void EndDialogue() {
        tutorial.dialogue.lines = null;
        ui.dialogueTypewriter.ShowText(string.Empty); // This just clears the text
        ui.dialogueTypewriter.gameObject.SetActive(false);
    }

    private void UpdateDialogue() {
        Dialogue dialogue = tutorial.dialogue;
        if (dialogue.Finished || dialogue.FadingOut) return;
        if (!input.advanceDialogue.WasPressedThisFrame()) return;

        TypewriterComponent typewriter = ui.dialogueTypewriter;
        if (typewriter.IsShowingText) {
            if (!dialogue.lines[dialogue.lineIndex].unskippable) {
                typewriter.SkipTypewriter();
            }
            return;
        }
        ShowNextDialogueLine();
    }

}
