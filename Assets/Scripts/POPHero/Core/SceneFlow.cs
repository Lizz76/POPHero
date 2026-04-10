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

        public void LoadBoot() => SceneManager.LoadScene(SceneNames.Boot);
        public void LoadMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

        public void LoadBattle()
        {
            Debug.Log("[POPHero] Loading Battle scene from main menu.");
            SceneManager.LoadScene(SceneNames.Battle);
        }

        public void ReloadBattle() => SceneManager.LoadScene(SceneNames.Battle);
    }
}
