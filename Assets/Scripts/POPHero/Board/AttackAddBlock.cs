namespace POPHero
{
    public class AttackAddBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball, float effectMultiplier)
        {
            game.RoundController.ProcessBlockHit(this, effectMultiplier);
        }
    }
}
