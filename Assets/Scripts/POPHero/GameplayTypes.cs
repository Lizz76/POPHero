namespace POPHero
{
    public enum RoundState
    {
        Aim,
        BallFlying,
        RoundResolve,
        BuffChoose,
        GameOver
    }

    public enum BoardBlockType
    {
        AttackAdd,
        AttackMultiply,
        Shield
    }

    public enum BuffEffectType
    {
        AttackAddAll,
        ShieldAddAll,
        MultiplierAddAll,
        UpgradeRandomAttack,
        UpgradeRandomShield,
        AddAttackBlock
    }

    public enum InputAimMode
    {
        MobileDragConfirm,
        PCMouseAimClick
    }

    public enum BlockVisualState
    {
        Default,
        Highlight,
        Dim
    }
}
