public class Ally : Piece
{
    public override void OnTurnEndOther()
    {
        base.OnTurnEndOther();
        shield = 0;
    }
}
