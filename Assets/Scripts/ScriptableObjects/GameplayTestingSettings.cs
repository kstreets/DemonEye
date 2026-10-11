using UnityEngine;

// Edited from the Gameplay Testing window. Lives in a Resources folder so it gets included in builds without being referenced
public class GameplayTestingSettings : ScriptableObject {

    public const string resourcesPath = "GameplayTestingSettings";

    [Tooltip("Replaces the player inventory with this when the game starts. Leave empty to keep the saved inventory")]
    public TestingInventory testingInventory;

}
