using UnityEngine;

[CreateAssetMenu(fileName = "Trader", menuName = "Scriptable Objects/Trader")]
public class Trader : ScriptableObject {

    public Sprite traderHeadshot;
    public string traderName;

    public State state;
    
    public class State {
        public int reputation;
        public int raidsUntilRestock;
    }

}
