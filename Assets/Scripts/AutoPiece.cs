using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AutoPiece : Piece
{
    [FormerlySerializedAs("enemyCards")]
    public List<Card> actionCards; // 순서대로 사용할 스킬/효과 리스트 (Enemy/AutoAlly 공용)

    int Movenum;

    Card nextMove;
    StunnedCard stunnedCard;
    public override void Awake()
    {
        base.Awake();
        Movenum = 0;
        nextMove = actionCards[0];
        stunnedCard = gameObject.AddComponent<StunnedCard>();
    }

    void Start()
    {
        ActionText();
    }

    public override List<Vector2Int> GetMoveableButton() {
        Card move = GetNextMove();
        if (move == null || move.effects.Count == 0) return base.GetMoveableButton();
        if (move.effects[0].type == EffectType.Move)
            return base.GetMoveableButton();
        // effectRange가 없는 카드는 이동 범위로 폴백하지 않고 그냥 아무 칸도 표시하지 않는다.
        return move.effects[0].effectRange?.GetAbleRange() ?? new List<Vector2Int>();
    }

    // 간단한 AI 로직: 다음에 사용할 카드를 반환. 기절 상태면 원래 예고했던 카드 대신 스턴 카드를 반환하고,
    // Movenum은 그대로 둬서(ChangeMove 미호출) 기절이 풀리면 원래 카드가 그대로 이어지게 한다.
    public virtual Card GetNextMove()
    {
        if (IsStunned()) return stunnedCard;
        if (actionCards == null || actionCards.Count == 0) return null;
        return nextMove;
    }

    public Card ChangeMove()
    {
        Movenum++;
        if (Movenum >= actionCards.Count)
            Movenum = 0;
        nextMove = actionCards[Movenum];
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

        pieceCanvas.ShowActionText(parts.Count > 0 ? string.Join(" ", parts) : card.Name);
    }

    string BuildEffectDescription(CardEffect effect)
    {
        string primary = effect.type switch
        {
            EffectType.Move        => "<sprite name=\"Move\">",
            EffectType.Damage      => effect.useColDamageAsDmg ? "<sprite name=\"Damage\">" : $"{effect.dmg + ColDamageDelta}<space=30><sprite name=\"Damage\">",
            EffectType.Shield      => $"{effect.dmg + ShieldBonusDelta}<space=30><sprite name=\"Shield\">",
            EffectType.Heal        => $"{effect.dmg}<space=30><sprite name=\"Heal\">",
            EffectType.SelfDamage  => $"{effect.dmg}<space=30><sprite name=\"Damage\">",
            EffectType.Draw        => effect.dmg > 1 ? $"{effect.dmg}" : "",
            EffectType.ApplyStatus => BuildStatusDescription(effect),
            EffectType.ColDamageUp => $"<sprite name=\"Damage\"><space=10>+{effect.dmg}",
            EffectType.ShieldBonusUp => $"<sprite name=\"Shield\"><space=10>+{effect.dmg}",
            EffectType.Stun        => "<sprite name=\"Stun\">",
            // Damage/Shield/Move/Heal/Positive/Negative 6개 아이콘 중 어디에도 안 맞는 효과(Charge 포함)는
            // 전용 아이콘 없이 자연스럽게 Unknown으로 표시한다.
            _                      => "<sprite name=\"Unknown\">"
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

    // 상태이상 종류(독/화상/강화 등)를 따로 표기하지 않고, 이로운 효과인지 해로운 효과인지만
    // Positive/Negative 아이콘으로 통일해서 보여준다. 아이콘 자체는 actIcon에 나중에 추가될 예정.
    static bool IsPositiveStatus(StatusEffectType type) => type switch
    {
        StatusEffectType.Regen or
        StatusEffectType.Strengthen or
        StatusEffectType.Thorn => true,
        _                      => false,
    };

    string BuildStatusDescription(CardEffect effect)
    {
        if (effect.statusEffectType == StatusEffectType.None) return "";
        if (effect.statusEffectType == StatusEffectType.Stun) return "<sprite name=\"Stun\">";
        return $"<sprite name=\"{(IsPositiveStatus(effect.statusEffectType) ? "Positive" : "Negative")}\">";
    }
}
