using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POPHero
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            var font = PrototypeVisualFactory.GetCjkTmpFontAsset();
            ApplyFont(titleLabel, font);
            ApplyFont(subtitleLabel, font);
            if (titleLabel != null)
                titleLabel.text = "POPHero";
            if (subtitleLabel != null)
                subtitleLabel.text = "弹珠构筑战斗原型";

            SetButtonLabel(startButton, "开始游戏", font);
            SetButtonLabel(quitButton, "退出游戏", font);

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => SceneFlowService.Instance.LoadBattle());
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void ApplyFont(TMP_Text label, TMP_FontAsset font)
        {
            if (label != null && font != null)
                label.font = font;
        }

        static void SetButtonLabel(Button button, string text, TMP_FontAsset font)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (font != null)
                    label.font = font;
                label.text = text;
            }
        }
    }
}
