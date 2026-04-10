using UnityEngine;
using UnityEngine.SceneManagement;

namespace POPHero
{
    public sealed class ProjectBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (SceneManager.GetActiveScene().name == SceneNames.Boot)
                SceneFlowService.Instance.LoadMainMenu();
        }
    }
}
