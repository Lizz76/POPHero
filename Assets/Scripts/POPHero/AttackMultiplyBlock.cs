namespace POPHero
{
    public class AttackMultiplyBlock : BoardBlock
    {
        protected override void OnBallHit(BallController ball)
        {
            game.RoundController.MultiplyAttack(valueA);
        }

        protected override string GetLabelText()
        {
            return $"x{valueA:0.0#}";
        }
    }
}
