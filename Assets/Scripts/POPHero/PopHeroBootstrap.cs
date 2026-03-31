using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public static class PopHeroBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            if (Object.FindObjectOfType<PopHeroGame>() != null)
                return;

            var root = new GameObject("POPHeroGame");
            root.AddComponent<PopHeroGame>();
        }
    }
}
