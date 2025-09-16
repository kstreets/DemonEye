using UnityEngine;

[CreateAssetMenu(fileName = "MechanicDesc", menuName = "Scriptable Objects/MechanicDesc")]
public class MechanicDesc : ScriptableObject {

    public string displayName;
    [TextArea] public string description;
    
}
