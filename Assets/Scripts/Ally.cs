public class Ally : Piece
{
    public override void Awake()
    {
        base.Awake();
        if (!isSummon) GameManager.instance?.AddAlly(gameObject);
    }

    private void OnDestroy()
    {
        if (!isSummon) GameManager.instance?.RemoveAlly(gameObject);
    }

    public override void OnTurnEndOther()
    {
        base.OnTurnEndOther();
        shield = 0;
    }
}
