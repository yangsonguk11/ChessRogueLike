using UnityEngine;

enum CardType
{
    Attack,
    Action,
}

enum TargetType
{
    Self,
    Enemy,
    Ally,
}
public abstract class Card : MonoBehaviour
{
    string Name, Description;
    int Cost;
    CardType type;
    TargetType target;

    public abstract bool CanUse();
    public abstract void Execute();

}
