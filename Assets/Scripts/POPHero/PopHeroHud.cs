using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public class PopHeroHud : MonoBehaviour
    {
        PopHeroGame game;
        GUIStyle boxStyle;
        GUIStyle titleStyle;
        GUIStyle textStyle;
        GUIStyle buttonStyle;
        GUIStyle badgeStyle;
        GUIStyle cardStyle;
        Texture2D panelTexture;
        Texture2D cardTexture;

        void Awake()
        {
            game = GetComponent<PopHeroGame>();
        }

        void OnGUI()
        {
            EnsureStyles();

            DrawStatusPanel();
            DrawRoundPanel();
            DrawDebugPanel();

            if (game.State == RoundState.BuffChoose)
                DrawBuffChoicePanel();

            if (game.State == RoundState.GameOver)
                DrawGameOverPanel();
        }

        void DrawStatusPanel()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 420f, 286f), boxStyle);
            GUILayout.Label("弹珠打怪原型", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label($"状态：{GetStateText(game.State)}", badgeStyle);
            GUILayout.Label($"瞄准模式：{game.CurrentAimModeLabel}", badgeStyle);
            GUILayout.Label($"等级：{game.Player.Level}", badgeStyle);
            GUILayout.Label($"击杀进度：{game.Player.KillsTowardNextLevel} / {(game.Player.IsMaxLevel ? "已满级" : game.Player.KillsRequiredForNextLevel.ToString())}", badgeStyle);
            GUILayout.Label($"方块数：{game.Player.AvailableBlockCount}", badgeStyle);
            GUILayout.Label($"敌人 #{game.EncounterIndex}：{game.CurrentEnemy?.DisplayName ?? "--"}", badgeStyle);
            GUILayout.Label($"敌人生命：{game.CurrentEnemy?.CurrentHp ?? 0}/{game.CurrentEnemy?.MaxHp ?? 0}", badgeStyle);
            GUILayout.Label($"敌人攻击：{game.CurrentEnemy?.AttackDamage ?? 0}", badgeStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"玩家生命：{game.Player.CurrentHp}/{game.Player.MaxHp}", textStyle);
            GUILayout.Label($"玩家护盾：{game.Player.CurrentShield}", textStyle);
            GUILayout.Label($"玩家金币：{game.Player.Gold}", textStyle);
            GUILayout.Space(6f);
            GUILayout.Label("护盾现在只在当前回合内有效。每轮都要重新规划输出和护盾，不再能靠跨回合护盾越滚越稳。", textStyle);
            GUILayout.EndArea();
        }

        void DrawRoundPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 360f, 16f, 344f, 324f), boxStyle);
            GUILayout.Label("战斗信息", titleStyle);
            GUILayout.Space(4f);

            if (game.State == RoundState.BallFlying)
            {
                GUILayout.Label($"本轮伤害：{game.RoundController.RoundAttackScore}", badgeStyle);
                GUILayout.Label($"本轮护盾：{game.RoundController.RoundShieldGain}", badgeStyle);
            }
            else if (game.State == RoundState.Aim)
            {
                GUILayout.Label("本轮伤害：发射后累计", badgeStyle);
                GUILayout.Label("本轮护盾：发射后累计", badgeStyle);
            }
            else
            {
                GUILayout.Label($"本轮伤害：{game.RoundController.RoundAttackScore}", badgeStyle);
                GUILayout.Label($"本轮护盾：{game.RoundController.RoundShieldGain}", badgeStyle);
            }

            GUILayout.Label($"命中次数：{game.RoundController.RoundHitCount}", badgeStyle);
            GUILayout.Label($"可发射数：{game.RemainingLaunchesForEnemy}/{game.MaxLaunchesPerEnemy}", badgeStyle);
            GUILayout.Label($"发射点 X：{game.CurrentLaunchPoint.x:0.00}", textStyle);
            GUILayout.Label($"当前场上方块：{game.BoardManager.ActiveBlocks.Count}", textStyle);
            GUILayout.Label($"方块池总数：{game.BoardManager.BlueprintCount}", textStyle);
            GUILayout.Space(8f);
            GUILayout.Label("已获得强化", titleStyle);
            var lines = game.BuffManager.GetAppliedBuffLines().ToArray();
            if (lines.Length == 0)
            {
                GUILayout.Label("暂时还没有永久强化。", textStyle);
            }
            else
            {
                foreach (var line in lines)
                    GUILayout.Label("- " + line, textStyle);
            }

            GUILayout.EndArea();
        }

        void DrawDebugPanel()
        {
            GUILayout.BeginArea(new Rect(16f, Screen.height - 176f, 760f, 160f), boxStyle);
            GUILayout.Label("操作与调试", titleStyle);
            GUILayout.Label("PC 模式：移动鼠标实时瞄准，左键点击后发射。手机模式：从发射点附近拖动定方向，松手后保留方向，再点一次确认发射。", textStyle);
            GUILayout.Label("敌人血量和伤害已经明显提高，护盾会在敌人出手后清空。升级后只会多解锁 1 个方块，不会再按回合自动送很多方块。", textStyle);
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("切换瞄准模式", buttonStyle, GUILayout.Width(140f)))
                game.ToggleAimMode();
            if (GUILayout.Button("重排方块", buttonStyle, GUILayout.Width(120f)))
                game.DebugShuffleBoard();
            if (GUILayout.Button("金币 +25", buttonStyle, GUILayout.Width(110f)))
                game.DebugAddGold(25);
            if (GUILayout.Button("击败敌人", buttonStyle, GUILayout.Width(110f)))
                game.DebugKillEnemy();
            if (GUILayout.Button("玩家 -10 生命", buttonStyle, GUILayout.Width(140f)))
                game.DebugDamagePlayer(10);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawBuffChoicePanel()
        {
            var choices = game.BuffManager.ActiveChoices;
            var panelWidth = Mathf.Min(960f, Screen.width - 48f);
            var cardWidth = (panelWidth - 60f) / Mathf.Max(1, choices.Count);
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - panelWidth * 0.5f, Screen.height * 0.5f - 170f, panelWidth, 320f), boxStyle);
            GUILayout.Label("选择 1 个强化", titleStyle);
            GUILayout.Label("敌人已被击败，奖励已经结算。选择强化后会进入下一只敌人与下一轮战斗。", textStyle);
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                GUILayout.BeginVertical(cardStyle, GUILayout.Width(cardWidth));
                GUILayout.Label(choice.name, titleStyle);
                GUILayout.Space(4f);
                GUILayout.Label(choice.description, textStyle, GUILayout.Height(92f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("选择强化", buttonStyle, GUILayout.Height(34f)))
                    game.TrySelectBuff(i);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawGameOverPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 100f, 400f, 196f), boxStyle);
            GUILayout.Label("游戏结束", titleStyle);
            GUILayout.Label(game.GameOverMessage, textStyle);
            GUILayout.Label("可以直接再来一次，重新尝试不同的成长路线、方块组合和回合规划。", textStyle);
            GUILayout.Space(10f);
            if (GUILayout.Button("再来一次", buttonStyle, GUILayout.Height(34f)))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            GUILayout.EndArea();
        }

        string GetStateText(RoundState state)
        {
            return state switch
            {
                RoundState.Aim => "瞄准",
                RoundState.BallFlying => "发射中",
                RoundState.RoundResolve => "结算中",
                RoundState.BuffChoose => "选择强化",
                RoundState.GameOver => "游戏结束",
                _ => state.ToString()
            };
        }

        void EnsureStyles()
        {
            if (boxStyle != null)
                return;

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.07f, 0.1f, 0.15f, 0.88f));
            panelTexture.Apply();

            cardTexture = new Texture2D(1, 1);
            cardTexture.SetPixel(0, 0, new Color(0.12f, 0.15f, 0.21f, 0.96f));
            cardTexture.Apply();

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                normal =
                {
                    textColor = Color.white,
                    background = panelTexture
                }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 28f
            };
            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.96f, 0.8f, 1f) }
            };
            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(8, 8, 0, 0),
                normal =
                {
                    textColor = Color.white,
                    background = cardTexture
                }
            };
        }
    }
}
