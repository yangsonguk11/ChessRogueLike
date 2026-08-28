using UnityEngine;

public class Enemy : AutoPiece
{
    public override void Awake()
    {
        base.Awake();
        if (!isSummon) GameManager.instance.AddEnemy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isSummon) GameManager.instance?.RemoveEnemy(gameObject);
    }
}
