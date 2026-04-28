using System;

namespace POPHero
{
    public sealed class HudCommandDispatcher
    {
        readonly PopHeroGame game;

        public HudCommandDispatcher(PopHeroGame game)
        {
            this.game = game;
        }

        public void Execute(HudCommand command)
        {
            if (game == null)
                return;

            switch (command.Type)
            {
                case HudCommandType.OpenSettings:
                    game.OpenSettings();
                    break;
                case HudCommandType.CloseSettings:
                    game.CloseSettings();
                    break;
                case HudCommandType.BackToMenu:
                    game.BackToMenu();
                    break;
                case HudCommandType.QuitGame:
                    game.QuitGame();
                    break;
                case HudCommandType.ToggleAimMode:
                    game.ToggleAimMode();
                    break;
                case HudCommandType.DebugShuffleBoard:
                    game.DebugShuffleBoard();
                    break;
                case HudCommandType.DebugAddGold:
                    game.DebugAddGold(command.IntValue);
                    break;
                case HudCommandType.DebugKillEnemy:
                    game.DebugKillEnemy();
                    break;
                case HudCommandType.DebugDamagePlayer:
                    game.DebugDamagePlayer(command.IntValue);
                    break;
                case HudCommandType.DebugTriggerMapNode:
                    game.DebugTriggerMapNode(command.PrimaryId);
                    break;
                case HudCommandType.DebugTriggerMapEventChoice:
                    game.DebugTriggerMapEventChoice(command.PrimaryId);
                    break;
                case HudCommandType.DiscardCurrentBall:
                    game.TryDiscardCurrentBall();
                    break;
                case HudCommandType.TrySelectBallReward:
                    game.TrySelectBallReward(command.IntValue);
                    break;
                case HudCommandType.TrySelectBlockReward:
                    game.TrySelectBlockReward(command.IntValue);
                    break;
                case HudCommandType.SkipBlockReward:
                    game.SkipBlockReward();
                    break;
                case HudCommandType.TrySelectReward:
                    game.TrySelectReward(command.IntValue);
                    break;
                case HudCommandType.TryRerollRewardChoices:
                    game.TryRerollRewardChoices();
                    break;
                case HudCommandType.SkipRewardChoices:
                    game.SkipRewardChoices();
                    break;
                case HudCommandType.TryBuyShopItem:
                    game.TryBuyShopItem(command.IntValue);
                    break;
                case HudCommandType.TryRerollShop:
                    game.TryRerollShop();
                    break;
                case HudCommandType.OpenBlockOperations:
                    var returnState = game.State;
                    if (!string.IsNullOrWhiteSpace(command.SecondaryId) &&
                        Enum.TryParse(command.SecondaryId, true, out RoundState parsedReturnState))
                        returnState = parsedReturnState;
                    game.OpenBlockOperations(command.PrimaryId, returnState);
                    break;
                case HudCommandType.CloseBlockOperations:
                    game.CloseBlockOperations();
                    break;
                case HudCommandType.CloseShop:
                    game.CloseShop();
                    break;
                case HudCommandType.FinishLoadout:
                    game.FinishLoadout();
                    break;
                case HudCommandType.BeginStickerDrag:
                    game.BeginStickerDrag(command.PrimaryId);
                    break;
                case HudCommandType.CancelStickerDrag:
                    game.CancelStickerDrag();
                    break;
                case HudCommandType.ToggleModActivation:
                    game.ToggleModActivation(command.PrimaryId);
                    break;
                case HudCommandType.TryRemoveBlock:
                    game.TryRemoveBlock(command.PrimaryId);
                    break;
                case HudCommandType.TryUpgradeBlock:
                    game.TryUpgradeBlock(command.PrimaryId);
                    break;
                case HudCommandType.TrySwapActiveReserve:
                    game.TrySwapActiveReserve(command.PrimaryId, command.SecondaryId);
                    break;
                case HudCommandType.TryInstallDraggedSticker:
                    if (game.TryInstallDraggedSticker(command.PrimaryId, command.IntValue, out var failReason))
                        game.SetIntermissionMessage("Sticker installed.");
                    else
                        game.SetIntermissionMessage(failReason);
                    break;
                case HudCommandType.RemoveStickerFromCard:
                    game.RemoveStickerFromCard(command.PrimaryId, command.IntValue);
                    break;
                case HudCommandType.SelectMapNode:
                    game.SelectMapNode(command.PrimaryId);
                    break;
                case HudCommandType.ChooseMapEventOption:
                    game.ChooseMapEventOption(command.IntValue);
                    break;
            }
        }
    }
}
