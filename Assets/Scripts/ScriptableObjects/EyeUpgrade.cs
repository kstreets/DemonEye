using UnityEngine;
using static ItemComponents;
using static Game;

[CreateAssetMenu(fileName = "EyeUpgrade", menuName = "Scriptable Objects/EyeUpgrade")]
public class EyeUpgrade : Item, IEyeUpgrade  {

    public MechanicDesc relativeMechanicDesc;
    
    public MapSpawning MapSpawning => mapSpawning;
    public UuidScriptableObject UuidObject => this;
    public Sprite InventorySprite => inventorySprite;
    public string DisplayName => displayName;
    
    public virtual void AddInstanceToEnemy(Enemy enemy, int stackCount) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) { }
}