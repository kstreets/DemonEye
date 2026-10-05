using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameData;

public partial class Game {

    // Where the UI should treat the "cursor" as being. Follows the controller selection when using a controller.
    private Vector2 PointerScreenPos => usingController ? controllNav.pointerPos : Mouse.current.position.ReadValue();
    private bool MenuNavigationNeeded => !InRaid || PlayerInventoryIsOpen || LootInventoryIsOpen;

    public class NavPanel {
        public RectTransform panel;
        public int priority; // Higher priority panels get selected over more recently used ones
        public Func<RectTransform> getDefaultSelection; // Used when nothing's been selected in the panel yet
        public RectTransform rememberedSelection;
        public bool alwaysCallDefault;
    }

    private void InitMenuNavigation() {
        controllNav.pointerEventData = new(EventSystem.current) { pointerId = ControllerNavigation.pointerEventId };
        
        input.menuMove = InputSystem.actions.FindAction("MenuMove");
        input.menuSubmit = InputSystem.actions.FindAction("MenuSubmit");
        input.menuTabLeft = InputSystem.actions.FindAction("MenuTabLeft");
        input.menuTabRight = InputSystem.actions.FindAction("MenuTabRight");
        input.menuSecondaryTabLeft = InputSystem.actions.FindAction("MenuSecondaryTabLeft");
        input.menuSecondaryTabRight = InputSystem.actions.FindAction("MenuSecondaryTabRight");
        input.escape.performed += OnEscapePressed;

        // Popups and the dragged item follow the pointer, so they shouldn't count as covering up what's under them
        controllNav.ignoredRoots = new[] {
            ui.dragAndDropItemUI.transform,
            ui.itemDescPopupInv.transform,
            ui.itemDescPopupPickup.transform,
            ui.mechanicDescPopup.transform,
            ui.hintPopup.transform,
        };

        // Panels with the same priority that haven't been used yet are picked in the order they're added here
        {
            const int selectionPriority = 1;
            AddNavPanel((RectTransform)mapPanels.confirmationPanel.transform, selectionPriority, () => mapPanels.confirmationPanel.teleportButton.rectTransform);
            AddNavPanel(mapPanels.mapSelectionPanel.rectTransform, selectionPriority, () => mapPanels.mapSelectionPanel.selectors[0].selectionButton.rectTransform);
            AddNavPanel(ui.lootInventoryPanel, selectionPriority, () => inventories.lootPtr.slots[0].ui.rectTransform, alwaysCallDefault: true);
            AddNavPanel(stashPanel.panel, selectionPriority, () => inventories.stash.slots[0].ui.rectTransform);
        }
        {
            const int selectionPriority = 0;
            AddNavPanel(playerPanel.panel, selectionPriority, () => PlayerInventoryIsSlim ? null : inventories.player.slots[playerEquipmentSize + playerQuickUseSize].ui.rectTransform);
            AddNavPanel(eyeForgePanel.panel, selectionPriority);
            AddNavPanel(eyeForgeDetailsPanel.panel, selectionPriority);
            AddNavPanel(traderPanel.panel, selectionPriority);
            AddNavPanel(transactionPanel.panel, selectionPriority);
            AddNavPanel(questsPanel.panel, selectionPriority);
            AddNavPanel(skillsPanel.panel.rectTransform, selectionPriority, () => skillsPanel.panel.hasteSkillRow.levelUpButton.rectTransform);
        }
    }

    private void AddNavPanel(RectTransform panel, int priority, Func<RectTransform> getDefaultSelection = null, bool alwaysCallDefault = false) {
        NavPanel navPanel = new() { 
            panel = panel, 
            priority = priority, 
            getDefaultSelection = getDefaultSelection,
            alwaysCallDefault = alwaysCallDefault,
        };
        controllNav.navPanels.Add(panel, navPanel);
        InsertIntoRecentPanels(navPanel, false);
    }

    private void OnEscapePressed(InputAction.CallbackContext context) {
        CancelItemDrag();
        ClearControllerSelection();
        
        // Don't want to be able to back out of tutorial sequence(s)
        if (!InSettings) {
            if (InTutorialFirstForge || (InHideout && InTutorial)) return;
        }
        
        if (ConfirmingMapSelection) {
            ShowMapSelectionUI();
            return;
        }
        if (InMapSelection || InHideout || InSettings) {
            states.gameStateMachine.SetState(states.mainMenu);
        }
        if (InRaid && PlayerInventoryIsOpen) {
            ClosePlayerInventory();
            CloseLootInventory();
        }
    }

    private void UpdateMenuNavigation() {
        if (!usingController) {
            // Tabs can still be switched with the bumpers/triggers while the Steam Deck's touchpad is controlling the cursor
            if (UsingControllerControls) {
                UpdateToggleGroupSwitching();
            }
            return;
        }
        
        // We do gamepad navigation ourselves so we need to clear Unity's to avoid double presses
        if (EventSystem.current && EventSystem.current.currentSelectedGameObject) {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.visible = false;

        if (states.gameStateMachine.CurState != controllNav.lastNavGameState) {
            controllNav.lastNavGameState = states.gameStateMachine.CurState;
            controllNav.hasPointerPos = false;
        }
        
        if (!MenuNavigationNeeded) {
            ClearControllerSelection();
            return;
        }
        
        if (controllNav.selected == null) {
            SelectDefaultNavTarget();
            return;
        }

        Vector2 navDir = ReadNavDirection();

        if (!IsValidNavTarget(controllNav.selected)) {
            SelectDefaultNavTarget();
            controllNav.lastNavDir = navDir;
            controllNav.repeatTimer = ControllerNavigation.repeatDelay;
        }
        else if (CanMoveNavigation(navDir)) {
            // Settings use left/right to change their value instead of moving the selection
            if (navDir.x != 0f && controllNav.selected.TryGetComponent(out SingleSetting setting)) {
                setting.OnHorizontalNav(navDir.x > 0f);
            }
            else {
                MoveControllerSelection(navDir);
            }
        }

        if (controllNav.selected) {
            controllNav.pointerPos = GetScreenRect(controllNav.selected).center;
            controllNav.hasPointerPos = true;
        }

        // The press that switched us over to the controller shouldn't also submit
        if (input.lastDeviceSwitchTime != Time.time) {
            UpdateControllerSubmit();
        }

        UpdateToggleGroupSwitching();
    }

    private void UpdateToggleGroupSwitching() {
        var switchedMode = CheckForToggleGroupSwitching();
        // We dont clear the selection when doing a secondary toggle switch, and there's no selection when using the cursor
        if (switchedMode == ToggleButtonGroup.NavigationMode.Primary && usingController) {
            ClearControllerSelection();
            SelectDefaultNavTarget();
        }
        if (switchedMode != ToggleButtonGroup.NavigationMode.None) {
            CancelItemDrag();
            SuppressInventoryPopup();
            HideInteractionPopup();
        }
    }

    // Whether the player moved the pointer this frame.
    // In order for this method to work reliably, the menu navigation must update before gamestate tick
    private bool PlayerMovedPointerThisFrame() {
        if (usingController) {
            return controllNav.lastTimePlayerMovedSelection == Time.time;
        }
        return Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero;
    }

    private void MenuNavigationOnInputDeviceChanged() {
        if (!usingController) {
            ClearControllerSelection();
            controllNav.submitPressedOn = null;
            Cursor.visible = MenuNavigationNeeded;
        }
        // Whatever was highlighted belongs to the other device
        ClearAllHighlights();
    }

    private void ClearAllHighlights() {
        foreach (Selectable selectable in Selectable.allSelectablesArray) {
            SendPointerExit(selectable.gameObject);
        }
        foreach (Inventory inventory in inventories.all) {
            foreach (InventorySlot slot in inventory.slots) {
                SendPointerExit(slot.ui.gameObject);
            }
        }
        
        static void SendPointerExit(GameObject gameObject) {
            foreach (IPointerExitHandler handler in gameObject.GetComponents<IPointerExitHandler>()) {
                handler.OnPointerExit(null);
            }
        }
    }

    private Vector2 ReadNavDirection() {
        Vector2 stick = input.menuMove.ReadValue<Vector2>();
        if (stick.magnitude < ControllerNavigation.stickDeadzone) return Vector2.zero;
        return Mathf.Abs(stick.x) > Mathf.Abs(stick.y) ? new(Mathf.Sign(stick.x), 0f) : new(0f, Mathf.Sign(stick.y));
    }

    // Ignores the stick until it's let go, for when it's already being held as a menu opens
    private void IgnoreHeldNavigationInput() {
        if (!usingController) return;
        controllNav.waitingForNavRelease = true;
    }

    private bool CanMoveNavigation(Vector2 navDir) {
        if (controllNav.waitingForNavRelease) {
            if (navDir != Vector2.zero) return false;
            controllNav.waitingForNavRelease = false;
        }

        if (navDir == Vector2.zero) {
            controllNav.lastNavDir = Vector2.zero;
            return false;
        }
        if (navDir != controllNav.lastNavDir) {
            controllNav.lastNavDir = navDir;
            controllNav.repeatTimer = ControllerNavigation.repeatDelay;
            return true;
        }

        controllNav.repeatTimer -= Time.unscaledDeltaTime;
        if (controllNav.repeatTimer > 0f) return false;
        controllNav.repeatTimer = ControllerNavigation.repeatInterval;
        return true;
    }

    private void MoveControllerSelection(Vector2 dir) {
        GatherNavCandidates();

        Rect fromRect = GetScreenRect(controllNav.selected);
        RectTransform targetRecTransform = null;
        float bestScore = float.MaxValue;

        foreach (RectTransform candidate in controllNav.possibleSelections) {
            if (candidate == controllNav.selected) continue;

            Rect to = GetScreenRect(candidate);
            Vector2 delta = to.center - fromRect.center;
            float along = Vector2.Dot(delta, dir);
            if (along <= 1f) continue;

            // How far off to the side the candidate is. Zero gap when the two line up, e.g. slots in the same row.
            bool horizontal = dir.x != 0f;
            float sideGap = horizontal ? IntervalGap(fromRect.yMin, fromRect.yMax, to.yMin, to.yMax) : IntervalGap(fromRect.xMin, fromRect.xMax, to.xMin, to.xMax);
            float sideOffset = Mathf.Abs(horizontal ? delta.y : delta.x);
            if (sideGap > 0f && sideOffset > along * 3f) continue;

            float score = along + sideGap * 3f + sideOffset * 0.5f;
            if (score < bestScore) {
                bestScore = score;
                targetRecTransform = candidate;
            }
        }

        if (targetRecTransform) {
            SetControllerSelection(targetRecTransform);
            controllNav.lastTimePlayerMovedSelection = Time.time;
        }
    }

    private void SelectDefaultNavTarget() {
        if (TryGetPanelSelection(out RectTransform panelSelection)) {
            SetControllerSelection(panelSelection);
            return;
        }

        GatherNavCandidates();

        RectTransform best = null;
        float bestScore = float.MaxValue;

        foreach (RectTransform candidate in controllNav.possibleSelections) {
            Vector2 center = GetScreenRect(candidate).center;
            // Stay near where we were (e.g. after switching hideout tabs), otherwise start top-left
            float score = controllNav.hasPointerPos ? (center - controllNav.pointerPos).sqrMagnitude : -center.y * 10000f + center.x;
            if (score < bestScore) {
                bestScore = score;
                best = candidate;
            }
        }

        if (best) {
            SetControllerSelection(best);
        }
        else {
            ClearControllerSelection();
        }
    }

    public void SetControllerSelection(RectTransform target) {
        controllNav.pointerEventData.Reset();
        if (controllNav.selected) {
            ExecuteEvents.Execute(controllNav.selected.gameObject, controllNav.pointerEventData, ExecuteEvents.pointerExitHandler);
        }
        controllNav.selected = target;
        RememberSelection(target);
        ScrollIntoView(target);
        // Move the pointer right away, otherwise hover checks before the next navigation update still see the old selection
        controllNav.pointerPos = GetScreenRect(target).center;
        controllNav.hasPointerPos = true;
        ExecuteEvents.Execute(target.gameObject, controllNav.pointerEventData, ExecuteEvents.pointerEnterHandler);
    }

    private void RememberSelection(RectTransform target) {
        // Walk up so the closest panel wins if panels are nested
        for (Transform parent = target; parent; parent = parent.parent) {
            if (!controllNav.navPanels.TryGetValue(parent, out NavPanel navPanel)) continue;
            navPanel.rememberedSelection = target;
            controllNav.recentPanels.Remove(navPanel);
            InsertIntoRecentPanels(navPanel, true);
            return;
        }
    }

    // Keeps the list sorted by priority, then goes either in front of or behind the panels with the same priority
    private void InsertIntoRecentPanels(NavPanel navPanel, bool mostRecent) {
        List<NavPanel> recentPanels = controllNav.recentPanels;
        int index = 0;
        while (index < recentPanels.Count) {
            int otherPriority = recentPanels[index].priority;
            bool goesBefore = mostRecent ? otherPriority <= navPanel.priority : otherPriority < navPanel.priority;
            if (goesBefore) break;
            index++;
        }
        recentPanels.Insert(index, navPanel);
    }

    // Goes to the highest priority panel that's showing, picking the most recently used one when priorities are tied.
    // Returns where we were in that panel, or its default if we haven't been there yet.
    private bool TryGetPanelSelection(out RectTransform selection) {
        foreach (NavPanel navPanel in controllNav.recentPanels) {
            if (!navPanel.panel || !navPanel.panel.gameObject.activeInHierarchy) continue;
            
            // Pooled objects that got deactivated or destroyed objects won't be valid anymore
            if (!navPanel.alwaysCallDefault && IsValidNavTarget(navPanel.rememberedSelection)) {
                selection = navPanel.rememberedSelection;
                return true;
            }

            RectTransform defaultSelection = navPanel.getDefaultSelection?.Invoke();
            if (IsValidNavTarget(defaultSelection)) {
                selection = defaultSelection;
                return true;
            }
        }
        selection = null;
        return false;
    }

    private void ClearControllerSelection() {
        if (!controllNav.selected) return;
        ExecuteEvents.Execute(controllNav.selected.gameObject, controllNav.pointerEventData, ExecuteEvents.pointerExitHandler);
        controllNav.selected = null;
    }

    // Simulates a mouse click on buttons. Inventory slots don't need this since the inventory actions have gamepad bindings.
    private void UpdateControllerSubmit() {
        if (input.menuSubmit.WasReleasedThisFrame() && controllNav.submitPressedOn) {
            GameObject pressed = controllNav.submitPressedOn;
            controllNav.submitPressedOn = null;
            ExecuteEvents.Execute(pressed, controllNav.pointerEventData, ExecuteEvents.pointerUpHandler);
            if (controllNav.selected && pressed == controllNav.selected.gameObject) {
                ExecuteEvents.Execute(pressed, controllNav.pointerEventData, ExecuteEvents.pointerClickHandler);
            }
        }

        if (!input.menuSubmit.WasPressedThisFrame()) return;
        if (!controllNav.selected || !controllNav.selected.TryGetComponent(out Selectable _)) return;

        controllNav.submitPressedOn = controllNav.selected.gameObject;
        ExecuteEvents.Execute(controllNav.submitPressedOn, controllNav.pointerEventData, ExecuteEvents.pointerDownHandler);
        // Selectable.OnPointerDown selects itself in the EventSystem, clear it right away so the input module doesn't also submit it
        if (EventSystem.current) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private ToggleButtonGroup.NavigationMode CheckForToggleGroupSwitching() {
        bool switchedPrimary = TrySwitchToggleGroup(ToggleButtonGroup.NavigationMode.Primary, input.menuTabLeft, input.menuTabRight);
        if (switchedPrimary) {
            return ToggleButtonGroup.NavigationMode.Primary;
        }
        bool switchedSecondary = TrySwitchToggleGroup(ToggleButtonGroup.NavigationMode.Secondary, input.menuSecondaryTabLeft, input.menuSecondaryTabRight);
        if (switchedSecondary) {
            return ToggleButtonGroup.NavigationMode.Secondary;
        }
        return ToggleButtonGroup.NavigationMode.None;
    }

    private bool TrySwitchToggleGroup(ToggleButtonGroup.NavigationMode mode, InputAction left, InputAction right) {
        int step = (right.WasPressedThisFrame() ? 1 : 0) - (left.WasPressedThisFrame() ? 1 : 0);
        if (step == 0) return false;

        ToggleButtonGroup group = GetActiveNavToggleGroup(mode);
        if (!group) return false;

        group.Move(step);
        return true;
    }

    // First group in the list thats on screen with this mode
    private ToggleButtonGroup GetActiveNavToggleGroup(ToggleButtonGroup.NavigationMode mode) {
        foreach (ToggleButtonGroup group in ui.navToggleGroups) {
            if (group && group.navigationMode == mode && group.gameObject.activeInHierarchy && group.TogglesUsable) {
                return group;
            }
        }
        return null;
    }

    private void GatherNavCandidates() {
        controllNav.possibleSelections.Clear();

        foreach (Selectable selectable in Selectable.allSelectablesArray) {
            RectTransform rectTransform = (RectTransform)selectable.transform;
            if (IsValidNavTarget(rectTransform)) {
                controllNav.possibleSelections.Add(rectTransform);
            }
        }

        foreach (Inventory inventory in inventories.all) {
            if (!inventory.parent.gameObject.activeInHierarchy) continue;
            foreach (InventorySlot slot in inventory.slots) {
                if (slot.ui && IsValidNavTarget(slot.ui.rectTransform)) {
                    controllNav.possibleSelections.Add(slot.ui.rectTransform);
                }
            }
        }
    }

    private bool IsValidNavTarget(RectTransform target) {
        if (!target || !target.gameObject.activeInHierarchy) return false;
        
        if (target.TryGetComponent(out Selectable selectable)) {
            if (!selectable.IsInteractable()) return false;
            if (selectable.navigation.mode == Navigation.Mode.None) return false;
        }
        
        foreach (Transform ignoreRoot in controllNav.ignoredRoots) {
            if (ignoreRoot && target.IsChildOf(ignoreRoot)) {
                return false;
            }
        }

        Rect rect = GetScreenRect(target);
        if (rect.width < 1f || rect.height < 1f) return false;

        // Things in scroll views can be off screen, we scroll to them when they get selected
        bool inScrollView = target.GetComponentInParent<ScrollRect>();
        bool onScreen = rect.center.x >= 0f && rect.center.y >= 0f && rect.center.x <= Screen.width && rect.center.y <= Screen.height;
        if (!onScreen && !inScrollView) return false;

        return true;
    }

    private void ScrollIntoView(RectTransform target) {
        ScrollRect scrollRect = target.GetComponentInParent<ScrollRect>();
        if (!scrollRect || !scrollRect.content) return;

        RectTransform viewport = scrollRect.viewport ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
        Rect view = viewport.rect;

        Vector2 offset = Vector2.zero;
        if (scrollRect.vertical) {
            if (bounds.max.y > view.yMax) offset.y = view.yMax - bounds.max.y;
            else if (bounds.min.y < view.yMin) offset.y = view.yMin - bounds.min.y;
        }
        if (scrollRect.horizontal) {
            if (bounds.max.x > view.xMax) offset.x = view.xMax - bounds.max.x;
            else if (bounds.min.x < view.xMin) offset.x = view.xMin - bounds.min.x;
        }
        if (offset == Vector2.zero) return;

        scrollRect.velocity = Vector2.zero;
        scrollRect.content.anchoredPosition += offset;
    }

    private Rect GetScreenRect(RectTransform rectTransform) {
        rectTransform.GetWorldCorners(controllNav.navCorners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, controllNav.navCorners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, controllNav.navCorners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static float IntervalGap(float aMin, float aMax, float bMin, float bMax) {
        return Mathf.Max(0f, Mathf.Max(aMin, bMin) - Mathf.Min(aMax, bMax));
    }

}
