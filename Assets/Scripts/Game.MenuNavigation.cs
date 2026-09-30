using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameData;

public partial class Game {

    // Where the UI should treat the "cursor" as being. Follows the controller selection when using a controller.
    private Vector2 PointerScreenPos => usingController ? controllNav.pointerPos : Mouse.current.position.ReadValue();
    private bool MenuNavigationNeeded => !InRaid || PlayerInventoryIsOpen || LootInventoryIsOpen;
    
    [NonSerialized] public bool usingController;

    private void InitMenuNavigation() {
        controllNav.pointerEventData = new(EventSystem.current) { pointerId = ControllerNavigation.pointerEventId };
        
        input.menuMove = InputSystem.actions.FindAction("MenuMove");
        input.menuSubmit = InputSystem.actions.FindAction("MenuSubmit");
        input.menuTabLeft = InputSystem.actions.FindAction("MenuTabLeft");
        input.menuTabRight = InputSystem.actions.FindAction("MenuTabRight");
        input.escape.performed += OnEscapePressed;

        // Popups and the dragged item follow the pointer, so they shouldn't count as covering up what's under them
        controllNav.ignoredRoots = new[] {
            ui.dragAndDropItemUI.transform,
            ui.itemDescPopupInv.transform,
            ui.itemDescPopupPickup.transform,
            ui.mechanicDescPopup.transform,
            ui.hintPopup.transform,
            hideoutTabs.parent,
        };

    }

    private void OnEscapePressed(InputAction.CallbackContext context) {
        CancelItemDrag();
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
        bool controllerJustActivated = CheckForControllerSwitch();
        if (!usingController) return;
        
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
            MoveControllerSelection(navDir);
        }

        if (controllNav.selected) {
            controllNav.pointerPos = GetScreenRect(controllNav.selected).center;
            controllNav.hasPointerPos = true;
        }

        if (!controllerJustActivated) {
            UpdateControllerSubmit();
        }
        
        bool switchedTabs = CheckForHideoutTabSwitching();
        if (switchedTabs) {
            HideInteractionPopup();
            HideInventoryItemPopup();
            // Goes back to what was last selected on the new tab, or its default the first time
            ClearControllerSelection();
            SelectDefaultNavTarget();
        }
    }

    // Returns true on the frame we switch over to the controller
    private bool CheckForControllerSwitch() {
        if (usingController) {
            Mouse mouse = Mouse.current;
            bool mouseUsed = mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
            if (!mouseUsed) return false;
            
            usingController = false;
            ClearControllerSelection();
            controllNav.submitPressedOn = null;
            Cursor.visible = MenuNavigationNeeded;
            ClearAllHighlights();
            return false;
        }

        bool joystickDetected = input.menuMove.ReadValue<Vector2>().magnitude > ControllerNavigation.stickDeadzone;
        bool controllerButtonPressed = input.menuSubmit.WasPressedThisFrame() || input.menuTabLeft.WasPressedThisFrame() || input.menuTabRight.WasPressedThisFrame();
        
        bool gamepadUsed = joystickDetected || controllerButtonPressed ;
        if (!gamepadUsed) return false;

        usingController = true;
        ClearAllHighlights();
        return true;
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

    // Moves once when the stick is first pushed, then repeats while held
    private bool CanMoveNavigation(Vector2 navDir) {
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
        }
    }

    private void SelectDefaultNavTarget() {
        if (TryGetRememberedSelection(out RectTransform remembered)) {
            SetControllerSelection(remembered);
            return;
        }
        if (SelectingMap) {
            SetControllerSelection(mapPanels.mapSelectionPanel.selectors[0].selectionButton.rectTransform);
            return;
        }
        if (ConfirmingMapSelection) {
            SetControllerSelection(mapPanels.confirmationPanel.teleportButton.rectTransform);
            return;
        }
        if (LootInventoryIsOpen) {
            SetControllerSelection(inventories.lootPtr.slots[0].ui.rectTransform);
            return;
        }
        if (PlayerInventoryIsOpen) {
            const int firstNormalSlot = playerEquipmentSize + playerQuickUseSize;
            SetControllerSelection(inventories.player.slots[firstNormalSlot].ui.rectTransform);
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
        if (TryGetRememberedSelectionKey(out object key)) {
            controllNav.rememberedSelections[key] = target;
        }
        ScrollIntoView(target);
        ExecuteEvents.Execute(target.gameObject, controllNav.pointerEventData, ExecuteEvents.pointerEnterHandler);
    }

    private bool TryGetRememberedSelectionKey(out object key) {
        key = null;
        if (InHideout) {
            key = hideoutTabs.toggleGroup.GetSelected();
        }
        if (InRaid && PlayerInventoryIsOpen && !LootInventoryIsOpen) {
            key = inventories.player;
        }
        if (SelectingMap) {
            key = mapPanels.mapSelectionPanel;
        }
        return key != null;
    }

    private bool TryGetRememberedSelection(out RectTransform selection) {
        selection = null;
        if (!TryGetRememberedSelectionKey(out object key)) {
            return false;
        }
        if (controllNav.rememberedSelections.TryGetValue(key, out selection)) {
            return IsValidNavTarget(selection); // In case it's not active anymore (pooled object) or got destroyed
        }
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

    private bool CheckForHideoutTabSwitching() {
        if (!InHideout || !hideoutTabs.parent.gameObject.activeInHierarchy) return false;
        int step = (input.menuTabRight.WasPressedThisFrame() ? 1 : 0) - (input.menuTabLeft.WasPressedThisFrame() ? 1 : 0);
        hideoutTabs.toggleGroup.Move(step);
        return step != 0;
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
        if (target.TryGetComponent(out Selectable selectable) && !selectable.IsInteractable()) return false;
        
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
