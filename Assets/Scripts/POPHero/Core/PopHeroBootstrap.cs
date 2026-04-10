using UnityEngine;

namespace POPHero
{
    /// <summary>
    /// Legacy fallback for battle scenes that forgot to place a PopHeroGame.
    /// Menu and boot scenes are expected to have no gameplay root.
    /// </summary>
    public static class PopHeroBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindObjectOfType<PopHeroGame>() != null)
                return;

            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != SceneNames.Battle)
                return;

            Debug.LogWarning("[POPHero] Battle scene is missing a PopHeroGame. " +
                             "Please place a PopHeroGame object in Battle.unity. " +
                             "Automatic runtime creation is disabled.");
        }
    }
}
