using System.Collections.Generic;
using UnityEngine;

public class Enemy : Piece
{
    Card c;
    public List<Card> enemyCards; // ���� ������ ��ų/ȿ�� ����Ʈ

    int Movenum;

    Card nextMove;
    public override void Awake()
    {
        base.Awake();
        Movenum = 0;
        nextMove = enemyCards[0];
        GameManager.instance.AddEnemy(gameObject);
    }

    void Start()
    {
        ActionText();
    }

    public override List<Vector2> GetMoveableButton() {
        Card move = GetNextMove();
        if (move == null || move.effects.Count == 0) return base.GetMoveableButton();
        if (move.effects[0].type == EffectType.Move)
            return base.GetMoveableButton();
        return move.effects[0].effectRange?.GetAbleRange() ?? base.GetMoveableButton();
    }

    // ������ AI ����: Ÿ�� ���� �� ȿ�� ��ȯ
    public virtual Card GetNextMove()
    {
        if (enemyCards == null || enemyCards.Count == 0) return null;
        return nextMove;
    }

    public Card ChangeMove()
    {
        Movenum++;
        if (Movenum >= enemyCards.Count)
            Movenum = 0;
        nextMove = enemyCards[Movenum];
        return nextMove;
    }
    public override void ActionText()
    {
        Card card = GetNextMove();
        if (card == null) return;

        var parts = new List<string>();
        foreach (CardEffect effect in card.effects)
        {
            string desc = BuildEffectDescription(effect);
            if (!string.IsNullOrEmpty(desc))
                parts.Add(desc);
        }

        pieceCanvas.ShowActionText(parts.Count > 0 ? string.Join(", ", parts) : card.Name);
    }

    string BuildEffectDescription(CardEffect effect)
    {
        string primary = effect.type switch
        {
            EffectType.Move        => "<sprite name=\"Move\">",
            EffectType.Damage      => effect.useColDamageAsDmg ? "<sprite name=\"Damage\">" : $"{effect.dmg} <sprite name=\"Damage\">",
            EffectType.Shield      => $"{effect.dmg} <sprite name=\"Shield\">",
            EffectType.Heal        => $"{effect.dmg} <sprite name=\"Heal\">",
            EffectType.SelfDamage  => $"{effect.dmg} <sprite name=\"Damage\">",
            EffectType.Draw        => effect.dmg > 1 ? $"{effect.dmg}" : "",
            EffectType.ApplyStatus => BuildStatusDescription(effect),
            EffectType.ColDamageUp => $"+{effect.dmg}",
            _                      => ""
        };

        // 주 효과에 상태이상이 함께 붙어있는 경우 표시
        if (effect.type != EffectType.ApplyStatus && effect.statusEffectType != StatusEffectType.None)
        {
            string statusDesc = BuildStatusDescription(effect);
            if (!string.IsNullOrEmpty(statusDesc))
                primary += $"+{statusDesc}";
        }

        return primary;
    }

    string BuildStatusDescription(CardEffect effect)
    {
        return effect.statusEffectType switch
        {
            StatusEffectType.Poison              => $"독({effect.statusPower})",
            StatusEffectType.Burning             => $"화상({effect.statusPower})",
            StatusEffectType.Regen               => $"재생({effect.statusPower})",
            StatusEffectType.Stun                => "기절",
            StatusEffectType.Strengthen          => $"강화({effect.statusPower})",
            StatusEffectType.Weaken              => $"약화({effect.statusPower})",
            StatusEffectType.TurnDamageStart or
            StatusEffectType.TurnDamageEnd       => $"턴 피해({effect.statusPower})",
            StatusEffectType.TurnAoEDamageStart or
            StatusEffectType.TurnAoEDamageEnd    => $"광역 피해({effect.statusPower})",
            StatusEffectType.Thorn               => $"가시({effect.statusPower})",
            _                                    => ""
        };
    }

    private void OnDestroy()
    {
        GameManager.instance.RemoveEnemy(gameObject);
    }
}
