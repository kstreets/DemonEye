using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public partial class Game {

    public static bool RollProbability(float probability) {
        return Random.value <= probability;
    }
    
    private Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);
    
    private bool InHideout => gameData.states.gameStateMachine.CurState == gameData.states.hideout;
    
    private bool InMapSelection => gameData.states.gameStateMachine.CurState == gameData.states.mapSelection;
    
    public bool InRaid => gameData.states.gameStateMachine.CurState == gameData.states.raid;

    public bool ControllerPluggedIn => Gamepad.current != null;

    private Vector3 RotationVector360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    private Vector3 RotationVector(float degrees) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right;
    }
    
    private Vector3 RotationVector(float degrees, float minDist, float maxDist) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }

    private Vector3 RandomizeVectorAngle(Vector3 vector, float degreeDelta) {
        return Quaternion.AngleAxis(Random.Range(-degreeDelta, degreeDelta), Vector3.forward) * vector;
    }
    
    private Quaternion RandomRotation() {
        return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
    }
    
    private Vector2 OffsetY(Vector2 pos, float yOffset) {
        return new(pos.x, pos.y + yOffset);
    }
    
    private Vector2 OffsetX(Vector2 pos, float xOffset) {
        return new(pos.x + xOffset, pos.y);
    }

    private float CurrentClipLength(Animator anim) {
        return anim.GetCurrentAnimatorStateInfo(0).length;
    }

    private string GetCountdownText(float timeLeft) {
        float time = Mathf.Clamp(timeLeft, 0f, float.MaxValue);
        int minutesLeft = Mathf.FloorToInt(time / 60f);
        int secondsLeft = Mathf.FloorToInt(time % 60f);
        return $"{minutesLeft:00}:{secondsLeft:00}";
    }

    private static string SizeText(string text, int fontSize) {
        return $"<size={fontSize}>{text}</size>";
    }
    
    public static string ColorText(string text, Color color) {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }
    
    public static string DisplayProb(float probability, Color? color = default) {
        Color textColor = color ?? Styles.instance.timeDescColor;
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", textColor);
    }
    
    public static string DisplayProbNoColor(float probability) {
        return $"{Mathf.FloorToInt(probability * 100f)}%";
    }

    public static string DisplayProbIncDec(float probability) {
        return probability >= 0f ? DisplayProbIncrease(probability) : DisplayProbDecrease(probability);
    }

    public static string DisplayProbIncrease(float probability, Color? color = default) {
        Color textColor = color ?? Styles.instance.increaseDescColor;
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", textColor);
    }
    
    public static string DisplayProbDecrease(float probability, Color? color = default) {
        Color textColor = color ?? Styles.instance.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(Mathf.FloorToInt(probability * 100f))}%", textColor);
    }

    public static string DisplayHealth(int health) {
        return ColorText(health.ToString(), Styles.instance.increaseDescColor);
    }

    public static string DisplayNumber(int number, Color? color = default) {
        Color textColor = color ?? Styles.instance.timeDescColor;
        return ColorText(number.ToString(), textColor);
    }
    
    public static string DisplayNumber(float number, Color? color = default) {
        Color textColor = color ?? Styles.instance.timeDescColor;
        return ColorText(number.ToString("0.00"), textColor);
    }
    
    public static string DisplayNumberNoColor(float number) {
        return number.ToString("0.00");
    }

    public static string DisplayIncDec(int amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(int amount, Color? color = default) {
        Color textColor = color ?? Styles.instance.increaseDescColor;
        return ColorText($"+{amount}", textColor);
    }
    
    public static string DisplayDecrease(int amount, Color? color = default) {
        Color textColor = color ?? Styles.instance.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(amount)}", textColor);
    }

    public static string DisplayIncDec(float amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(float amount, Color? color = default) {
        Color textColor = color ?? Styles.instance.increaseDescColor;
        return ColorText($"+{amount:0.00}", textColor);
    }
    
    public static string DisplayDecrease(float amount, Color? color = default) {
        Color textColor = color ?? Styles.instance.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(amount):0.00}", textColor);
    }
    
    public static string DisplayMultiplier(float multiplier, Color? color = default) {
        Color textColor = multiplier >= 1f ? Styles.instance.increaseDescColor : Styles.instance.decreaseDescColor;
        return ColorText($"{multiplier:0.00}x", color ?? textColor);
    }
    
    public static string DisplayMultiplierNoColor(float multiplier) {
        return $"{multiplier:0.00}x";
    }

    public static string DisplayMultiplierIncDec(float multiplier) {
        return multiplier >= 0f ? DisplayMultiplierIncrease(multiplier) : DisplayMultiplierDecrease(multiplier);
    }

    public static string DisplayMultiplierIncrease(float multiplier, Color? color = default) {
        Color textColor = color ?? Styles.instance.increaseDescColor;
        return ColorText($"+{multiplier:0.00}x", textColor);
    }
    
    public static string DisplayMultiplierDecrease(float multiplier, Color? color = default) {
        Color textColor = color ?? Styles.instance.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(multiplier):0.00}x", textColor);
    }

    public static string DisplaySeconds(float time) {
        if (time == 1f) {
            return ColorText($"{time:0}<space=0.12em>s", Styles.instance.timeDescColor);
        }
        
        bool isWholeNumber = time % 1 == 0;
        if (isWholeNumber) {
            return ColorText($"{time:0}<space=0.12em>s", Styles.instance.timeDescColor);
        }
        
        return ColorText($"{time:0.0#}<space=0.12em>s", Styles.instance.timeDescColor);
    }
    
    public static int TaperInteger(int value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return Mathf.RoundToInt(value * Mathf.Pow(stackCount, taper));
    }

    public static float TaperFloat(float value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return value * Mathf.Pow(stackCount, taper);
    }

    private enum CardinalDir { Right, Left, Up, Down }

    private CardinalDir CardinalDirFromVector(Vector2 vector) {
        float dot = Vector2.Dot(Vector2.right, vector.normalized);
        if (Mathf.Abs(dot) >= 0.2f) {
            return vector.x > 0 ? CardinalDir.Right : CardinalDir.Left;
        } 
        return vector.y > 0 ? CardinalDir.Up : CardinalDir.Down;
    }
    
}
