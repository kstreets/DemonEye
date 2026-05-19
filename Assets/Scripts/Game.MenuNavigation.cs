using UnityEngine;
using UnityEngine.InputSystem;

public partial class Game {

    private void InitMenuNavigation() {
        var menuMove = InputSystem.actions.FindAction("MenuMove");
        menuMove.performed += OnMoveInput;
        gameData.input.escape.performed += OnEscapePressed;
    }
    
    private void OnEscapePressed(InputAction.CallbackContext context) {
        if (InMapSelection || InHideout) {
            gameData.states.gameStateMachine.SetState(gameData.states.mainMenu);
        }
        if (InRaid && InventoryIsOpen) {
            ClosePlayerInventory();
            CloseLootInventory(); 
        }
    }
    
    Vector2 controllerPos;
    
    private void OnMoveInput(InputAction.CallbackContext context) {
        if (!context.performed) return;
        
        GameObject[,] mainMenuGrid = new GameObject[4, 1];
        mainMenuGrid[0, 0] = gameData.mainMenu.playButton.gameObject;
        mainMenuGrid[1, 0] = gameData.mainMenu.hideoutButton.gameObject;
        mainMenuGrid[2, 0] = gameData.mainMenu.settingsButton.gameObject;
        mainMenuGrid[3, 0] = gameData.mainMenu.exitButton.gameObject;
        
        Vector2 dir = context.ReadValue<Vector2>();
        dir = new(dir.x, -dir.y);
        
        if (gameData.states.gameStateMachine.CurState == gameData.states.mainMenu) {
            if (!mainMenuGrid.IndexInRange(controllerPos + dir)) return;
            controllerPos += dir;
            
            GameObject selected = mainMenuGrid[(int)controllerPos.y, (int)controllerPos.x];
            HightlightControllerSelection(selected);
            print(selected.gameObject.name);
        }
    }

    private GameObject currentlyHiglighted;
    
    private void HightlightControllerSelection(GameObject selectedGameObject) {
        if (currentlyHiglighted) {
            DehighlightControllerSelection(currentlyHiglighted);     
        }
        
        if (selectedGameObject.TryGetComponent(out ButtonFeel button)) {
            button.OnPointerEnter(null);
        }
        
        currentlyHiglighted = selectedGameObject;
    }
    
    private void DehighlightControllerSelection(GameObject selectedGameObject) {
        if (selectedGameObject.TryGetComponent(out ButtonFeel button)) {
            button.OnPointerExit(null);
        }
    }
    
}
