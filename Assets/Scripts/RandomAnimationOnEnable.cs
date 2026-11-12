using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RandomAnimationOnEnable : MonoBehaviour {

    public string[] stateNames;

    private Animator animator;
    private int[] stateHashes;

    private void Awake() {
        animator = GetComponent<Animator>();
        stateHashes = new int[stateNames.Length];
        for (int i = 0; i < stateNames.Length; i++) {
            stateHashes[i] = Animator.StringToHash(stateNames[i]);
        }
    }

    private void OnEnable() {
        if (stateHashes.Length == 0) return;
        animator.Play(stateHashes[Random.Range(0, stateHashes.Length)], 0, 0f);
        animator.Update(0f); // Apply now so the current state info (e.g. one shot lifetimes) reads the chosen clip
    }

}
