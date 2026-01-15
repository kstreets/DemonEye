using System;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

[Serializable]
public class FixedBitSet256 {
    
    public const int bitCount = 256;
    private const int blockCount = bitCount / 64;
    [SerializeField] private ulong b0, b1, b2, b3;

    public FixedBitSet256() { }

    public FixedBitSet256(int count, bool defaultValue) {
        ClearAll();
        for (int i = 0; i < count; i++) {
            if (defaultValue == true) {
                Set(i);
            }
        }
    }

    public bool IsSet(int index) {
        int block = index >> 6;
        int bit = index & 63;
        ulong mask = 1UL << bit;

        return block switch {
            0 => (b0 & mask) != 0,
            1 => (b1 & mask) != 0,
            2 => (b2 & mask) != 0,
            _ => (b3 & mask) != 0,
        };
    }

    public void Set(int index) {
        int block = index >> 6;
        int bit = index & 63;
        ulong mask = 1UL << bit;

        switch (block) {
            case 0: b0 |= mask; break;
            case 1: b1 |= mask; break;
            case 2: b2 |= mask; break;
            default: b3 |= mask; break;
        }
    }

    public void Clear(int index) {
        int block = index >> 6;
        int bit = index & 63;
        ulong mask = ~(1UL << bit);

        switch (block) {
            case 0: b0 &= mask; break;
            case 1: b1 &= mask; break;
            case 2: b2 &= mask; break;
            default: b3 &= mask; break;
        }
    }

    public void ClearAll() {
        b0 = b1 = b2 = b3 = 0;
    }

    public void SetAll() {
        b0 = b1 = b2 = b3 = ulong.MaxValue;
    }

    public int Count() => math.countbits(b0) + math.countbits(b1) + math.countbits(b2) + math.countbits(b3);

    public void And(in FixedBitSet256 other) {
        b0 &= other.b0;
        b1 &= other.b1;
        b2 &= other.b2;
        b3 &= other.b3;
    }

    public void Or(in FixedBitSet256 other) {
        b0 |= other.b0;
        b1 |= other.b1;
        b2 |= other.b2;
        b3 |= other.b3;
    }

    public int RandomSetIndex() {
        int rand = Random.Range(0, Count());
        int foundCount = -1;
        for (int i = 0; i < bitCount; i++) {
            if (IsSet(i)) {
                foundCount++;
            }
            if (rand == foundCount) {
                return i;
            }
        }
        return -1;
    }
}
