using UnityEngine;

// 적 전용: 시전자 중심 범위(effectRange[0]) 안의 상대 기물 전원의 전투 덱에 addCardID 카드를 1장씩 넣는다.
// 전투 덱(Piece.pieceDeck)에만 들어가므로 전투가 끝나면 사라진다. 넣을 카드와 더미(Hand/Deck/Discard)는
// 프리팹에서 바꾼다. 예시 적 Enemy-WoundKnight이 쓴다.
public class EnemyAddCardCard : Card
{
    public string addCardID = "WoundCard";
    public CardPositionZone addCardZone = CardPositionZone.Hand;

    public override void Awake()
    {
        base.Awake();
        Name = "EnemyAddCardCard";
        Cost = 0;
        type = CardType.Skill;
        user = User.Enemy;

        effects.Add(new CardEffect
        {
            requiredMode = Board.BoardMode.command,
            type = EffectType.AddCard,
            addCardID = addCardID,
            addCardZone = addCardZone,
            targetlogic = TargetLogic.AllEnemiesInRange,
            effectRange = effectRange.Count > 0 ? effectRange[0] : null,
            areaTargetMode = AreaTargetMode.Fixed,
            animTrigger = "ApplyStatus",
        });
    }

    string ZoneName => addCardZone switch
    {
        CardPositionZone.Hand => "손패",
        CardPositionZone.Deck => "덱",
        _ => "버린 카드 더미",
    };

    public override string EffectDescription => $"모든 적의 {ZoneName}에 {addCardID} 1장을 넣습니다.";
}
