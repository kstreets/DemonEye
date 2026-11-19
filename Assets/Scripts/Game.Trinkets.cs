using JetBrains.Annotations;

public static partial class Game {
    
    public struct TrinketData {
        public Duration activeDuration;
        public Duration cooldownDuration;
        public int trackingCount;
    }

    static void OnEquipTrinket([CanBeNull] Trinket trinket) {
        trinkets.data.Reset();
        trinkets.equiped = trinket;
    }
    
}
