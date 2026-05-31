using System.Collections.Generic;
using UnityEngine;

public class Boss : Enemy
{
    // Boss stays fixed at center — no move card in enemyCards, so it never moves.
    // The 3 boss cards (Cross, Spin, AreaShield) cycle in sequence via Enemy.ChangeMove().
}
