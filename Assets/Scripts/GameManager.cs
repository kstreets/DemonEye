using UnityEngine;

public class GameManager : MonoBehaviour {

    public Transform player;
    
    private void Start() {
        Player.Init(player);
    }

    private void Update() {
        Player.Update();
    }
    
}
