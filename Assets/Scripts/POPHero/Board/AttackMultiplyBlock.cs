namespace POPHero
{
    public class AttackMultiplyBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball, float effectMultiplier)
        {
            game.RoundController.ProcessBlockHit(this, effectMultiplier);
        }
    }
}
