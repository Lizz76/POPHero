namespace POPHero
{
    public class AttackMultiplyBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.ProcessBlockHit(this);
        }
    }
}
