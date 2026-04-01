using System.Collections.Generic;
using UnityEngine;

namespace POPHero
{
    public class ModData
    {
        public string id;
        public string name;
        public string description;
        public ModCategory category;
        public float valueA;
    }

    public class ModInstance
    {
        public string runtimeId;
        public ModData data;
        public bool isActive;
    }

    public class ModManager
    {
        readonly List<ModData> catalog = new();
        readonly List<ModInstance> ownedMods = new();
        readonly List<ModInstance> activeMods = new();
        readonly List<ModInstance> reserveMods = new();

        PopHeroGame game;
        int serial;

        public IReadOnlyList<ModInstance> OwnedMods => ownedMods;
        public IReadOnlyList<ModInstance> ActiveMods => activeMods;
        public IReadOnlyList<ModInstance> ReserveMods => reserveMods;

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            serial = 0;
            ownedMods.Clear();
            activeMods.Clear();
            reserveMods.Clear();
            BuildCatalog();
        }

        public int ModUseCap => Mathf.Max(1, game.config.mods.modUseCap);

        public ModData GetRandomUnownedMod()
        {
            var candidates = catalog.FindAll(data => !ownedMods.Exists(mod => mod.data.id == data.id));
            return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
        }

        public void AcquireMod(string modId)
        {
            var data = catalog.Find(mod => mod.id == modId);
            if (data == null || ownedMods.Exists(mod => mod.data.id == modId))
                return;

            serial += 1;
            var instance = new ModInstance
            {
                runtimeId = $"mod_{serial:0000}",
                data = data,
                isActive = activeMods.Count < ModUseCap
            };

            ownedMods.Add(instance);
            if (instance.isActive)
                activeMods.Add(instance);
            else
                reserveMods.Add(instance);

            ApplyImmediateModEffect(instance);
        }

        public void ToggleActivation(string runtimeId)
        {
            var active = activeMods.Find(mod => mod.runtimeId == runtimeId);
            if (active != null)
            {
                active.isActive = false;
                activeMods.Remove(active);
                reserveMods.Add(active);
                return;
            }

            var reserve = reserveMods.Find(mod => mod.runtimeId == runtimeId);
            if (reserve == null || activeMods.Count >= ModUseCap)
                return;

            reserve.isActive = true;
            reserveMods.Remove(reserve);
            activeMods.Add(reserve);
        }

        public bool HasActive(string modId)
        {
            return activeMods.Exists(mod => mod.data.id == modId);
        }

        public float GetAimAssistBonus()
        {
            return HasActive("aim_assist") ? 1f : 0f;
        }

        public float GetStableAimBonus()
        {
            return HasActive("stable_aim") ? 0.25f : 0f;
        }

        public float GetFastFingerMultiplier()
        {
            return HasActive("fast_fingers") ? 0.8f : 1f;
        }

        public float GetSlowFingerMultiplier()
        {
            return HasActive("slow_fingers") ? 1.2f : 1f;
        }

        public bool ShowHitCounter()
        {
            return HasActive("hit_counter");
        }

        public bool ShowTrajectoryMemory()
        {
            return HasActive("trajectory_memory");
        }

        public int GetInventoryCapacityBonus()
        {
            return HasActive("hold_more") ? 2 : 0;
        }

        public int GetRewardChoiceCount()
        {
            return HasActive("rich_choice") ? 4 : 3;
        }

        public int GetShopRerollDiscount()
        {
            return HasActive("cheap_reroll") ? 1 : 0;
        }

        public float GetRewardGoldMultiplier()
        {
            return HasActive("more_money") ? 1.25f : 1f;
        }

        public int GetInterestIncome(int currentGold)
        {
            return HasActive("interest") ? Mathf.FloorToInt(currentGold / 25f) : 0;
        }

        public float GetStickerPowerMultiplier(BlockCardState card, StickerInstance instance)
        {
            if (!HasActive("same_sticker_bonus") || card == null || instance?.data == null)
                return 1f;

            var familyMatches = 0;
            foreach (var socket in card.sockets)
            {
                if (socket.installedSticker?.data?.family == instance.data.family)
                    familyMatches += 1;
            }

            return familyMatches >= 2 ? 1.25f : 1f;
        }

        void ApplyImmediateModEffect(ModInstance instance)
        {
            switch (instance.data.id)
            {
                case "socket_plus":
                    game.BoardManager.UnlockRandomSocket();
                    break;
            }
        }

        void BuildCatalog()
        {
            catalog.Clear();
            catalog.Add(Make("aim_assist", "Aim Assist", "稍微放宽锁输入的吸附与保持范围。", ModCategory.Information));
            catalog.Add(Make("hit_counter", "Hit Counter", "瞄准时显示更详细的命中统计。", ModCategory.Information));
            catalog.Add(Make("trajectory_memory", "Trajectory Memory", "保留上一条锁定路线的短暂残影。", ModCategory.Information));
            catalog.Add(Make("more_money", "More Money", "击败敌人时额外获得 25% 金币。", ModCategory.Economy));
            catalog.Add(Make("interest", "Interest", "每回合结束按当前金币获得少量利息。", ModCategory.Economy));
            catalog.Add(Make("cheap_reroll", "Cheap Reroll", "商店刷新费用 -1。", ModCategory.Economy));
            catalog.Add(Make("fast_fingers", "Fast Fingers", "切换到新瞄准路线更灵敏。", ModCategory.Operation));
            catalog.Add(Make("slow_fingers", "Slow Fingers", "锁定路线更稳，不容易因手抖切线。", ModCategory.Operation));
            catalog.Add(Make("stable_aim", "Stable Aim", "角度阈值更宽，锁定瞄准更稳定。", ModCategory.Operation));
            catalog.Add(Make("hold_more", "Hold More", "库存容量增加。", ModCategory.Growth));
            catalog.Add(Make("socket_plus", "+1 Socket", "随机一张载体卡立刻多 1 个可用槽位。", ModCategory.Growth));
            catalog.Add(Make("rich_choice", "Rich Choice", "奖励从 3 选 1 提升为 4 选 1。", ModCategory.Growth));
            catalog.Add(Make("same_sticker_bonus", "Same Sticker Bonus", "同 family 嵌片装在同一卡片上时效果更强。", ModCategory.Build));
        }

        static ModData Make(string id, string name, string description, ModCategory category)
        {
            return new ModData
            {
                id = id,
                name = name,
                description = description,
                category = category
            };
        }
    }

    public class ShopManager
    {
        readonly List<ShopItemEntry> items = new();
        readonly List<GrowthRewardData> growthPool = new();

        PopHeroGame game;

        public IReadOnlyList<ShopItemEntry> Items => items;
        public ShopEventState EventState { get; private set; } = ShopEventState.Hidden;
        public string LastFeedback { get; private set; } = string.Empty;
        public bool InShop { get; private set; }
        public bool HasRemovedBlockThisVisit { get; private set; }

        public void Initialize(PopHeroGame owner)
        {
            game = owner;
            items.Clear();
            growthPool.Clear();
            growthPool.Add(new GrowthRewardData { id = "shop_socket", name = "扩展槽位", description = "随机一张载体卡解锁 1 个槽位。", rewardType = GrowthRewardType.UnlockSocket, value = 1 });
            growthPool.Add(new GrowthRewardData { id = "shop_inventory", name = "扩容盒", description = "嵌片库存上限 +1。", rewardType = GrowthRewardType.IncreaseInventoryCapacity, value = 1 });
            growthPool.Add(new GrowthRewardData { id = "shop_launch", name = "备用弹珠", description = "每只敌人的可发射数 +1。", rewardType = GrowthRewardType.IncreaseLaunchCapacity, value = 1 });
        }

        public void OpenShop()
        {
            EventState = ShopEventState.ShopWillAppear;
            InShop = true;
            HasRemovedBlockThisVisit = false;
            GenerateItems();
        }

        public void CloseShop()
        {
            EventState = ShopEventState.ShopDisappear;
            InShop = false;
            HasRemovedBlockThisVisit = false;
            LastFeedback = string.Empty;
        }

        public void GenerateItems()
        {
            items.Clear();
            EventState = ShopEventState.ShopItemsGenerated;
            LastFeedback = string.Empty;

            for (var i = 0; i < 3; i++)
            {
                var sticker = game.StickerCatalog.GetRandomSticker();
                if (sticker != null)
                    items.Add(new ShopItemEntry { id = $"shop_sticker_{i}_{sticker.id}", kind = ShopItemKind.Sticker, title = sticker.name, description = sticker.mainActionText, stickerData = sticker, price = GetStickerPrice(sticker) });
            }

            var mod = game.ModManager.GetRandomUnownedMod();
            if (mod != null)
                items.Add(new ShopItemEntry { id = $"shop_mod_{mod.id}", kind = ShopItemKind.Mod, title = mod.name, description = mod.description, modData = mod, price = 14 });

            var growth = growthPool[Random.Range(0, growthPool.Count)];
            items.Add(new ShopItemEntry { id = $"shop_growth_{growth.id}", kind = ShopItemKind.Growth, title = growth.name, description = growth.description, growthData = growth, price = 11 });
        }

        public bool TryBuy(int index)
        {
            if (index < 0 || index >= items.Count)
                return false;

            var item = items[index];
            if (item.purchased)
                return false;

            EventState = ShopEventState.TryToSpendMoney;
            if (game.Player.Gold < item.price)
            {
                EventState = ShopEventState.ShopNoMoney;
                LastFeedback = "金币不足。";
                return false;
            }

            game.Player.SpendGold(item.price);
            item.purchased = true;
            ApplyItem(item);
            EventState = ShopEventState.ShopBuySuccess;
            LastFeedback = $"{item.title} 已购买。";
            return true;
        }

        public bool TryReroll()
        {
            EventState = ShopEventState.ShopShuffle;
            var cost = Mathf.Max(1, game.config.shop.shopRerollMoney - game.ModManager.GetShopRerollDiscount());
            if (game.Player.Gold < cost)
            {
                EventState = ShopEventState.ShopNoMoney;
                LastFeedback = "金币不足，无法刷新商店。";
                return false;
            }

            game.Player.SpendGold(cost);
            GenerateItems();
            LastFeedback = "商店已刷新。";
            return true;
        }

        public bool TryRemoveBlock(string cardId)
        {
            if (!InShop)
                return false;

            if (HasRemovedBlockThisVisit)
            {
                LastFeedback = "这次商店已经删过 1 张方块了。";
                return false;
            }

            if (game.Player.Gold < game.config.shop.blockRemovalCost)
            {
                LastFeedback = "金币不足，无法删除方块。";
                return false;
            }

            if (!game.BoardManager.TryRemoveOwnedCard(cardId, out var failReason))
            {
                LastFeedback = failReason;
                return false;
            }

            game.Player.SpendGold(game.config.shop.blockRemovalCost);
            HasRemovedBlockThisVisit = true;
            LastFeedback = "方块已删除。";
            return true;
        }

        void ApplyItem(ShopItemEntry item)
        {
            switch (item.kind)
            {
                case ShopItemKind.Sticker:
                    var sticker = game.StickerCatalog.CreateInstance(item.stickerData.id);
                    if (sticker != null && !game.StickerInventory.TryAdd(sticker))
                    {
                        game.Player.IncreaseInventoryCapacity(1);
                        game.StickerInventory.TryAdd(sticker);
                    }
                    break;
                case ShopItemKind.Mod:
                    game.ModManager.AcquireMod(item.modData.id);
                    break;
                case ShopItemKind.Growth:
                    game.ApplyGrowthReward(item.growthData);
                    break;
            }
        }

        static int GetStickerPrice(StickerData data)
        {
            return data.rarity switch
            {
                StickerRarity.Common => 6,
                StickerRarity.Uncommon => 9,
                StickerRarity.Rare => 12,
                StickerRarity.Epic => 16,
                _ => 8
            };
        }
    }
}
