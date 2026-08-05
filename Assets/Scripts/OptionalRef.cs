using UnityEngine.Assertions;

public struct OptionalRef<T> where T : struct {
    
    private T value;
    private bool hasValue;
    
    public bool HasValue => hasValue;
    public static implicit operator OptionalRef<T>(T value) => new (value);
    
    public OptionalRef(T value) {
        this.value = value;
        hasValue = true;
    }
    
    public void ClearValue() {
        hasValue = false;
    }

    public static ref T GetValueByReference(ref OptionalRef<T> optionalRef) {
        Assert.IsTrue(optionalRef.HasValue, $"Calling {nameof(GetValueByReference)} on null {nameof(OptionalRef<T>)}");
        return ref optionalRef.value;
    }
    
}

public static class OptionalRefExtensions {
    
    public static ref T GetValue<T>(this ref OptionalRef<T> optionalRef) where T : struct {
        return ref OptionalRef<T>.GetValueByReference(ref optionalRef);
    }
    
}
