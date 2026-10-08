using System;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameData;

public partial class Game {

    private bool InTutorial => tutorial.stateMachine != null && !tutorial.stateMachine.OnLastState;
    private bool InTutorialFirstForge => InTutorial && tutorial.stateMachine.NotPassedThisState(tutorial.waitingToEnterSlaughterMap);
    private bool InTutorialSlaughterMap => InTutorial && !InTutorialFirstForge && tutorial.stateMachine.NotPassedThisState(tutorial.diedInSlaughterMap);
    private bool InTutorialFirstTraderMeeting => InTutorial && !InTutorialSlaughterMap && tutorial.stateMachine.NotPassedThisState(tutorial.firstTraderMeeting);
    private bool InTutorialHideoutTour => InTutorial && !InTutorialFirstTraderMeeting && !tutorial.stateMachine.OnLastState;

    // Set from the Gameplay Testing window
    public const string skipTutorialEditorPrefKey = "DemonEye_SkipTutorial";

    private void InitTutorial(GameState gameState) {
        bool compltedTutorial = gameState != null && gameState.tutorialStateIndex == -1;
        if (compltedTutorial) return;

#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool(skipTutorialEditorPrefKey)) {
            SkipTutorial();
            return;
        }
#endif

        mainMenu.hideoutButton.SetClickableState(false);
        tutorial.dialogueTypewriter = ui.openingDialogueTypewriter;
        
        int restoreHideoutInputPadding = inputPrompts.hideoutParent.GetComponent<HorizontalLayoutGroup>().padding.left;
        string restoreTeleportingIntoRaidText = ui.teleportingIntoRaidHeader.text;
        
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
                ui.teleportingIntoRaidHeader.text = "Craft the Demon Eye";
                FadeIn(ui.teleportingIntoRaidHeader, 1f);
                inputPrompts.hideoutParent.gameObject.GetComponent<HorizontalLayoutGroup>().padding.left = 50;
                inputPrompts.hideoutParent.gameObject.SetActive(true);
            });
        });
        tutorial.craftingDemonEyeState = tutorial.stateMachine.CreateState(enter: () => {
            ui.teleportingIntoRaidHeader.text = string.Empty;
            inventories.stash.isLocked = true;
            inventories.eyeForge.isLocked = true;
            FadeOutAndCollapsePanel(stashPanel.panel, 1f, 1.2f);
        });
        tutorial.equipingDemonEyeState = tutorial.stateMachine.CreateState(enter: () => {
            ui.teleportingIntoRaidHeader.text = "Equip the Demon Eye";
            FadeIn(ui.teleportingIntoRaidHeader, 1f);
            ToggleSlimPlayerPanel(false);
            FadeInAndExpandPanel(playerPanel.panel, 1f, 1.2f)
            .OnComplete(() => inventories.eyeForge.isLocked = false);
            inventories.player.slots[0].ui.SetOutlined(true);
        }, exit: () => {
            inventories.player.slots[0].ui.SetOutlined(false);
        });
        tutorial.waitingToEnterSlaughterMap = tutorial.stateMachine.CreateState(enter: () => {
            inventories.player.isLocked = true;
            
            FadeOutAndCollapsePanel(eyeForgePanel.panel, 1f, 1.2f);
            
            TextMeshProUGUI headerText = ui.teleportingIntoRaidHeader;
            headerText.gameObject.gameObject.SetActive(true);

            const int countdownSeconds = 5;
            Sequence countdown = Sequence.Create();
            for (int secondsLeft = countdownSeconds; secondsLeft > 0; secondsLeft--) {
                string text = $"Teleporting into Raid in {secondsLeft}";
                countdown.ChainCallback(() => headerText.text = text);
                countdown.Chain(Tween.Scale(headerText.rectTransform, 1.1f, 1f, 1.2f, Ease.OutQuad));
            }
            countdown.ChainCallback(() => {
                Tween.Scale(headerText.rectTransform, 1.1f, 1f, 1.2f, Ease.OutQuad);
                headerText.text = restoreTeleportingIntoRaidText; // Map selection uses the same header
                LoadMapAsync(config.tutorialSlaughterMap);
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
            mainMenuSequence.Complete();
            ShowMainMenuUI();
            
            mainMenu.playButton.SetClickableState(false);
            mainMenu.hideoutButton.SetClickableState(true);
            
            mainMenuSequence.isPaused = true;
            states.gameStateMachine.Pause();
            
            FadeIn(ui.traderTutorialDialogueCanvasGroup, 1f);
            tutorial.dialogueTypewriter = ui.traderTutorialTypewriter;
            
            StartDialogue(
                onFinished: () => {
                    ui.traderTutorialDialogueBox.SetActive(false);
                    mainMenuSequence.isPaused = false;
                    states.gameStateMachine.UnPause();
                },
                Line("Well... you're not going to get far when your raids look like that one. I think you could benefit from my services..."),
                Line("I run a business where I help Sinoculus Demons reach ascension."),
                Line("Here, just come to the Hideout and I'll explain more.")
            );
            SaveGameState();
        });
        tutorial.firstHideoutVisit = tutorial.stateMachine.CreateState(enter: () => {
            mainMenu.hideoutButton.SetClickableState(true);
            mainMenu.hideoutNotifier.SetActive(true);
            mainMenu.playButton.SetClickableState(false);
        });
        tutorial.hideoutCharacter = tutorial.stateMachine.CreateState(enter: () => {
            ui.traderTutorialDialogueBox.SetActive(true);
            tutorial.dialogueTypewriter = ui.traderTutorialTypewriter; // Set again in case of restoring from a save
            hideoutTabs.toggleGroup.SetTogglesHidden(false);
            hideoutTabs.toggleGroup.SetTogglesLocked(true);

            if (!persistentFlags.HasFlag(PersistentFlags.HideoutTourItemsGiven)) {
                foreach (ItemWithCount itemWithCount in config.hideoutTourStartingInventory.itemsWithCounts) {
                    TryAddItemToInventory(inventories.stash, itemWithCount.item, itemWithCount.count);
                }
                persistentFlags |= PersistentFlags.HideoutTourItemsGiven;
            }

            StartDialogue(
                Line("Welcome to the Hideout! I provide all the utilities you could need on your journey to ascension."),
                Line("Here we have the Inventory tab, a place where you can stash items, heal up, and prepare your body in various ways for raids."),
                Line("Anything you place in the Stash is safe. So when returning from a raid, make sure to transfer all your items into it."),
                Line("Ok lets move onto the Crafting tab.")
            );
        });
        tutorial.hideoutForge = tutorial.stateMachine.CreateState(enter: () => {
            FadeInHideout();
            hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.eyeForgeButton);
            StartDialogue(
                Line("Here lies the Pentagram where you can craft all the Demon Eyes your little heart desires. Eyeballs and Blood Runes not included.")
            );
        });
        tutorial.hideoutTrader = tutorial.stateMachine.CreateState(enter: () => {
            FadeInHideout();
            hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.traderButton);
            StartDialogue(
                Line("My personal favorite tab, Trading. Oh look there I am!"),
                Line("This is where you can sell and trade items. Most of the items I trade can be found in raid, but sometimes it's better to just get them from me."),
                Line("Now with all these free perks, you might be wondering what the catch is?")
            );
        });
        tutorial.hideoutQuests = tutorial.stateMachine.CreateState(enter: () => {
            FadeInHideout();
            hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.questsButton);
            StartDialogue(
                Line("Indentured servitude! In exchange for the Hideout and help along your journey towards ascension, you have to do whatever I say. Otherwise known as quests."),
                Line("This tab is where I post all the things I want you to do. As an incentive for being a good demon, the more quests you complete, the more items I'll stock for you.")
            );
        });
        tutorial.hideoutSkills = tutorial.stateMachine.CreateState(enter: () => {
            hideoutTabs.toggleGroup.ManualyToggle(hideoutTabs.skillsButton);
            StartDialogue(
                Line("Finally, the last tab, Skills. Here you can permanently upgrade your stats by sacrificing the souls of those you killed."),
                Line("Thats the tour, have fun in your future raids!"),
                Line("Oh and make sure to check the Quests tab, I already have some postings for you there. Make sure to read the contents, they are filled with my wisdom.")
            );
        });
        tutorial.completed = tutorial.stateMachine.CreateState(enter: () => {
            hideoutTabs.toggleGroup.SetTogglesLocked(false);
            ui.traderTutorialDialogueBox.gameObject.SetActive(false);
            inputPrompts.hideoutParent.gameObject.SetActive(true);
            ui.menuBackButton.gameObject.SetActive(true);
            mainMenu.playButton.SetClickableState(true);
            SaveGameState();
        });
        
        tutorial.entryState.To(tutorial.openingDialogue).When(() => InHideout);
        tutorial.openingDialogue.To(tutorial.firstCraftingState).When(() => tutorial.dialogue.Finished);
        tutorial.firstCraftingState.To(tutorial.craftingDemonEyeState).When(() => PlayingForgeAnimation);
        tutorial.craftingDemonEyeState.To(tutorial.equipingDemonEyeState).When(() => !PlayingForgeAnimation).WithDelay(0.5f);
        tutorial.equipingDemonEyeState.To(tutorial.waitingToEnterSlaughterMap).When(() => inventories.player.slots[0].itemInstance?.isDemonEye ?? false);
        tutorial.waitingToEnterSlaughterMap.To(tutorial.inSlaughterMap).When(() => curRaid.mapLoadingState is MapLoadingState.Loading or MapLoadingState.Loaded);
        tutorial.inSlaughterMap.To(tutorial.diedInSlaughterMap).When(() => player.health <= 0f);
        tutorial.diedInSlaughterMap.To(tutorial.firstTraderMeeting).When(() => ShowingMainMenu);
        tutorial.firstTraderMeeting.To(tutorial.firstHideoutVisit).When(() => tutorial.dialogue.Finished).WithDelay(1f);
        tutorial.firstHideoutVisit.To(tutorial.hideoutCharacter).When(() => InHideout);
        tutorial.hideoutCharacter.To(tutorial.hideoutForge).When(() => tutorial.dialogue.Finished);
        tutorial.hideoutForge.To(tutorial.hideoutTrader).When(() => tutorial.dialogue.Finished);
        tutorial.hideoutTrader.To(tutorial.hideoutQuests).When(() => tutorial.dialogue.Finished);
        tutorial.hideoutQuests.To(tutorial.hideoutSkills).When(() => tutorial.dialogue.Finished);
        tutorial.hideoutSkills.To(tutorial.completed).When(() => tutorial.dialogue.Finished);

        bool restoringTutorialFromSaveIndex = gameState != null;
        if (restoringTutorialFromSaveIndex) {
            State lastSaveState = tutorial.stateMachine.StateFromIndex(gameState.tutorialStateIndex);
            // The save right before teleporting into the Slaughter map happens while still waiting to enter it,
            // so the Demon Eye was already equipped and we restore into the map instead of redoing the first forge
            if (lastSaveState == tutorial.waitingToEnterSlaughterMap) {
                lastSaveState = tutorial.inSlaughterMap;
            }
            tutorial.stateMachine.SetStateWithoutCallbacks(lastSaveState);

            if (InTutorialFirstForge) {
                ClearInventory(inventories.stash);
                ClearInventory(inventories.eyeForge);
                ClearInventory(inventories.player);
                tutorial.stateMachine.SetState(tutorial.entryState);
            } else if (InTutorialSlaughterMap) {
                player.health = FullPlayerHealth();
            } else if (InTutorialFirstTraderMeeting) {
                tutorial.stateMachine.SetStateWithoutCallbacks(tutorial.diedInSlaughterMap);
            } else if (InTutorialHideoutTour) {
                tutorial.stateMachine.SetState(tutorial.firstHideoutVisit);
            }
        }
    }

#if UNITY_EDITOR
    // Leaves the game as if the tutorial was just finished. Without a tutorial state machine the game isn't in the tutorial,
    // so the next save marks it as completed and it won't come back even if skipping is turned off again.
    private void SkipTutorial() {
        // Same items the hideout tour hands out, which is everything a player has once the tutorial is done
        if (!persistentFlags.HasFlag(PersistentFlags.HideoutTourItemsGiven)) {
            foreach (ItemWithCount itemWithCount in config.hideoutTourStartingInventory.itemsWithCounts) {
                TryAddItemToInventory(inventories.stash, itemWithCount.item, itemWithCount.count);
            }
            persistentFlags |= PersistentFlags.HideoutTourItemsGiven;
        }
        Debug.Log("Tutorial skipped from the Gameplay Testing window");
    }
#endif

    private void UpdateTutorial() {
        if (!InTutorial) return;
        tutorial.stateMachine.Tick();
    }
    
    private void TutorialOnHideoutEnter() {
        if (!InTutorial) return;
        
        if (InTutorialHideoutTour) {
            inputPrompts.hideoutParent.gameObject.SetActive(false);
            ui.menuBackButton.gameObject.SetActive(false);
            eyeForgePanel.toggleButtonGroup.SetTogglesHidden(false);
            FadeInHideout();
            return;
        }
        
        if (InTutorialFirstForge) {
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
        public Action finishedCallback;
        public bool Finished => lines == null;
        public bool FadingOut => fadeOutTween.isAlive;
    }

    private static DialogueLine Line(string text, bool unskippable = false) => new() { text = text, unskippable = unskippable };

    private void StartDialogue(params DialogueLine[] lines) => StartDialogue(fadeOutTime: 0f, onFinished: null, lines);
    private void StartDialogue(float fadeOutTime, params DialogueLine[] lines) => StartDialogue(fadeOutTime: fadeOutTime, onFinished: null, lines);
    private void StartDialogue(Action onFinished, params DialogueLine[] lines) => StartDialogue(fadeOutTime: 0f, onFinished: onFinished, lines);

    private void StartDialogue(float fadeOutTime, Action onFinished = null, params DialogueLine[] lines) {
        Dialogue dialogue = tutorial.dialogue;
        dialogue.lines = lines;
        dialogue.lineIndex = -1;
        dialogue.finishedCallback = onFinished;
        dialogue.fadeOutTime = fadeOutTime;
        dialogue.fadeOutTween.Stop();
        DialogueCanvasGroup().alpha = 1f;
        tutorial.dialogueTypewriter.gameObject.SetActive(true);
        ShowNextDialogueLine();
    }

    private CanvasGroup DialogueCanvasGroup() {
        GameObject typewriterObject = tutorial.dialogueTypewriter.gameObject;
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
        tutorial.dialogueTypewriter.ShowText(dialogue.lines[dialogue.lineIndex].text);
    }

    private void EndDialogue() {
        tutorial.dialogue.finishedCallback?.Invoke();
        tutorial.dialogue.lines = null;
        tutorial.dialogueTypewriter.ShowText(string.Empty); // This just clears the text
        tutorial.dialogueTypewriter.gameObject.SetActive(false);
    }

    private void UpdateDialogue() {
        Dialogue dialogue = tutorial.dialogue;
        if (dialogue.Finished || dialogue.FadingOut) return;
        if (!input.advanceDialogue.WasPressedThisFrame()) return;

        TypewriterComponent typewriter = tutorial.dialogueTypewriter;
        if (typewriter.IsShowingText) {
            if (!dialogue.lines[dialogue.lineIndex].unskippable) {
                typewriter.SkipTypewriter();
            }
            return;
        }
        ShowNextDialogueLine();
    }

}
