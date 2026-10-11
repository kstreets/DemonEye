using System;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using UnityEngine;

public partial class Game {

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

}
