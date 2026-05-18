public abstract class StatusEffect
{
    public abstract string DisplayName { get; }
    public abstract bool IsBuff { get; }
    public int duration;

    public virtual void OnApply(Piece piece) { }

    // Returns false when the effect expires (duration ran out)
    public virtual bool OnTurnEnd(Piece piece)
    {
        duration--;
        return duration > 0;
    }

    public virtual void OnRemove(Piece piece) { }
}
