namespace POPHero
{
    public class AttackAddBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.ProcessBlockHit(this);
        }
    }
}
