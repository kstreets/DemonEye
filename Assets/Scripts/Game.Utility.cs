using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public static partial class Game {
    static void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static StringBuilder _stringBuilder = new();
    
    public static StringBuilder GetStringBuilder() {
        _stringBuilder.Clear();
        return _stringBuilder;
    }
    
    public static bool RollProbability(float probability) {
        return Random.value <= probability;
    }

    static Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);
    static Vector2Int ScreenSize => new(Screen.width, Screen.height);
    static bool InHideout => states.gameStateMachine.CurState == states.hideout;
    static bool InMapSelection => states.gameStateMachine.CurState == states.mapSelection;
    static bool SelectingMap => InMapSelection && mapPanels.mapSelectionPanel.gameObject.activeInHierarchy;
    static bool ConfirmingMapSelection => InMapSelection && mapPanels.confirmationPanel.gameObject.activeInHierarchy;
    static bool InSettings => states.gameStateMachine.CurState == states.settingsMenu;
    
    public static bool InRaid => states.gameStateMachine.CurState == states.raid;

    static Vector3 RotationVector360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    public static Vector3 RotationVector(float degrees) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right;
    }
    
    public static Vector3 RotationVector(float degrees, float minDist, float maxDist) {
        return Quaternion.AngleAxis(degrees, Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }

    static Vector3 RandomizeVectorAngle(Vector3 vector, float degreeDelta) {
        return Quaternion.AngleAxis(Random.Range(-degreeDelta, degreeDelta), Vector3.forward) * vector;
    }

    static Quaternion RandomRotation() {
        return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
    }

    static Quaternion LookRotation(Vector2 lookDir, Vector2 entityForward = default) {
        entityForward = entityForward == default ? Vector2.right : entityForward;
        return Quaternion.AngleAxis(Vector2.SignedAngle(entityForward.normalized, lookDir.normalized), Vector3.forward);
    }

    static Vector2 OffsetY(Vector2 pos, float yOffset) {
        return new(pos.x, pos.y + yOffset);
    }

    static Vector2 OffsetX(Vector2 pos, float xOffset) {
        return new(pos.x + xOffset, pos.y);
    }

    static string GetCountdownText(float timeLeft) {
        float time = Mathf.Clamp(timeLeft, 0f, float.MaxValue);
        int minutesLeft = Mathf.FloorToInt(time / 60f);
        int secondsLeft = Mathf.FloorToInt(time % 60f);
        return $"{minutesLeft:00}:{secondsLeft:00}";
    }
    
    public static int GetTextMeshRomanNumeralIndex(int num) {
        const int startingIndex = 6;
        return startingIndex + (num - 1);
    }

    static string SizeText(string text, int fontSize) {
        return $"<size={fontSize}>{text}</size>";
    }
    
    public static string ColorText(string text, Color color) {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }
    
    public static string ColorSprite(int spriteIndex, Color color) {
        return $"<sprite index={spriteIndex} color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
    }
    
    public static string DisplayProb(float probability, Color? color = default) {
        Color textColor = color ?? config.styles.timeDescColor;
        return ColorText($"{Mathf.FloorToInt(probability * 100f)}%", textColor);
    }
    
    public static string DisplayProbNoColor(float probability) {
        return $"{Mathf.FloorToInt(probability * 100f)}%";
    }

    public static string DisplayProbIncDec(float probability) {
        return probability >= 0f ? DisplayProbIncrease(probability) : DisplayProbDecrease(probability);
    }

    public static string DisplayProbIncrease(float probability, Color? color = default) {
        Color textColor = color ?? config.styles.increaseDescColor;
        return ColorText($"+{Mathf.FloorToInt(probability * 100f)}%", textColor);
    }
    
    public static string DisplayProbDecrease(float probability, Color? color = default) {
        Color textColor = color ?? config.styles.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(Mathf.FloorToInt(probability * 100f))}%", textColor);
    }

    public static string DisplayHealth(int health) {
        return ColorText(health.ToString(), config.styles.increaseDescColor);
    }

    public static string DisplayNumber(int number, Color? color = default) {
        Color textColor = color ?? config.styles.timeDescColor;
        return ColorText(number.ToString(), textColor);
    }
    
    public static string DisplayNumber(float number, Color? color = default) {
        Color textColor = color ?? config.styles.timeDescColor;
        return ColorText(number.ToString("0.00"), textColor);
    }
    
    public static string DisplayNumberNoColor(float number) {
        return number.ToString("0.00");
    }

    public static string DisplayIncDec(int amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(int amount, Color? color = default) {
        Color textColor = color ?? config.styles.increaseDescColor;
        return ColorText($"+{amount}", textColor);
    }
    
    public static string DisplayDecrease(int amount, Color? color = default) {
        Color textColor = color ?? config.styles.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(amount)}", textColor);
    }

    public static string DisplayIncDec(float amount) {
        return amount >= 0f ? DisplayIncrease(amount) : DisplayDecrease(amount);
    }

    public static string DisplayIncrease(float amount, Color? color = default) {
        Color textColor = color ?? config.styles.increaseDescColor;
        return ColorText($"+{amount:0.00}", textColor);
    }
    
    public static string DisplayDecrease(float amount, Color? color = default) {
        Color textColor = color ?? config.styles.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(amount):0.00}", textColor);
    }
    
    public static string DisplayMultiplier(float multiplier, Color? color = default) {
        Color textColor = multiplier >= 1f ? config.styles.increaseDescColor : config.styles.decreaseDescColor;
        return ColorText($"{multiplier:0.00}x", color ?? textColor);
    }
    
    public static string DisplayMultiplierNoColor(float multiplier) {
        return $"{multiplier:0.00}x";
    }

    public static string DisplayMultiplierIncDec(float multiplier) {
        return multiplier >= 0f ? DisplayMultiplierIncrease(multiplier) : DisplayMultiplierDecrease(multiplier);
    }

    public static string DisplayMultiplierIncrease(float multiplier, Color? color = default) {
        Color textColor = color ?? config.styles.increaseDescColor;
        return ColorText($"+{multiplier:0.00}x", textColor);
    }
    
    public static string DisplayMultiplierDecrease(float multiplier, Color? color = default) {
        Color textColor = color ?? config.styles.decreaseDescColor;
        return ColorText($"-{Mathf.Abs(multiplier):0.00}x", textColor);
    }

    public static string DisplaySeconds(float time) {
        if (time == 1f) {
            return ColorText($"{time:0}<space=0.12em>s", config.styles.timeDescColor);
        }
        
        bool isWholeNumber = time % 1 == 0;
        if (isWholeNumber) {
            return ColorText($"{time:0}<space=0.12em>s", config.styles.timeDescColor);
        }
        
        return ColorText($"{time:0.0#}<space=0.12em>s", config.styles.timeDescColor);
    }
    
    public static string DisplayDistance(float dist) {
        float distPerUnit = config.gameplay.distancePerUnit;
        float unitDist =  dist / distPerUnit;
        return ColorText($"{unitDist:0.0}<space=0.12em>m", config.styles.timeDescColor);
    }
    
    public static int TaperInteger(int value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return Mathf.RoundToInt(value * Mathf.Pow(stackCount, taper));
    }

    public static float TaperFloat(float value, int stackCount, float taper) {
        Assert.IsFalse(taper >= 1f && taper <= 0f, "Taper needs to be between 0 and 1");
        return value * Mathf.Pow(stackCount, taper);
    }

    public enum CardinalDir { Right, Left, Up, Down }

    static CardinalDir CardinalDirFromVector(Vector2 vector) {
        float dot = Vector2.Dot(Vector2.right, vector.normalized);
        if (Mathf.Abs(dot) >= 0.3f) {
            return vector.x > 0 ? CardinalDir.Right : CardinalDir.Left;
        } 
        return vector.y > 0 ? CardinalDir.Up : CardinalDir.Down;
    }
    
}
