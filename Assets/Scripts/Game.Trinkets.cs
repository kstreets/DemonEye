using JetBrains.Annotations;

public partial class Game {
    
    public struct TrinketData {
        public Duration activeDuration;
        public Duration cooldownDuration;
        public int trackingCount;
    }
    
    private void OnEquipTrinket([CanBeNull] Trinket trinket) {
        trinkets.data.Reset();
        trinkets.equiped = trinket;
    }
    
}
