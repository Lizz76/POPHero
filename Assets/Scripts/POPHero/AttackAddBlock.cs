using UnityEngine;

namespace POPHero
{
    public class AttackAddBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.AddAttack(Mathf.RoundToInt(valueA));
        }

        protected override string GetLabelText()
        {
            return $"+{Mathf.RoundToInt(valueA)}";
        }
    }
}
