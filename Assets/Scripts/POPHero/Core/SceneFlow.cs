using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Battle = "Battle";
    }

    public sealed class SceneFlowService
    {
        static SceneFlowService instance;

        public static SceneFlowService Instance => instance ??= new SceneFlowService();

        public void LoadBoot()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Boot);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public void LoadBattle()
        {
            Time.timeScale = 1f;
            Debug.Log("[POPHero] Loading Battle scene from main menu.");
            SceneManager.LoadScene(SceneNames.Battle);
        }

        public void ReloadBattle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Battle);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
