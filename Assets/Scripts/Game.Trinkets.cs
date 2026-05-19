public partial class Game {
    
    public struct TrinketData {
        public Duration activeDuration;
        public Duration cooldownDuration;
        public int trackingCount;
    }
    
    private void OnEquipTrinket(Trinket trinket) {
        trinkets.data.Reset();
        trinkets.equiped = trinket;
    }
    
}
