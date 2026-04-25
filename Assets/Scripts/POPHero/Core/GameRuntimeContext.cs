namespace POPHero
{
    public sealed class GameRuntimeContext
    {
        public PopHeroPrototypeConfig Config { get; set; }
        public ConfigTableService Tables { get; set; }
        public PlayerData Player { get; set; }
        public BoardManager Board { get; set; }
        public RoundController Round { get; set; }
        public StickerCatalog StickerCatalog { get; set; }
        public StickerInventory StickerInventory { get; set; }
        public StickerEffectRunner StickerEffectRunner { get; set; }
        public RewardChoiceController RewardChoices { get; set; }
        public ModManager Mods { get; set; }
        public ShopManager Shop { get; set; }
        public ICombatEventHub CombatEvents { get; set; }
    }
}
