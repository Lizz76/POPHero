using UnityEngine;

namespace POPHero
{
    public class ShieldBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.AddShield(Mathf.RoundToInt(valueA));
        }

        protected override string GetLabelText()
        {
            return $"+{Mathf.RoundToInt(valueA)}";
        }
    }
}
