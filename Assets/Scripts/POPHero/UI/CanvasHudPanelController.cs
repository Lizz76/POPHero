using System;
using UnityEngine;

namespace POPHero
{
    internal sealed class CanvasHudPanelController
    {
        readonly Action refreshAction;

        public CanvasHudPanelController(string name, Action refreshAction)
        {
            Name = name;
            this.refreshAction = refreshAction;
        }

        public string Name { get; }

        public void Refresh()
        {
            try
            {
                refreshAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[POPHero] Canvas HUD refresh failed in {Name}: {ex.Message}");
            }
        }
    }
}
