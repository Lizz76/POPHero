using UnityEngine;

namespace POPHero
{
    public class AttackAddBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.ProcessBlockHit(this);
        }

        protected override string GetLabelText()
        {
            return $"+{Mathf.RoundToInt(valueA)}";
        }
    }
}
