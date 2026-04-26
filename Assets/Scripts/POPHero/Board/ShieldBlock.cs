namespace POPHero
{
    public class ShieldBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball, float effectMultiplier)
        {
            game.RoundController.ProcessBlockHit(this, effectMultiplier);
        }
    }
}
