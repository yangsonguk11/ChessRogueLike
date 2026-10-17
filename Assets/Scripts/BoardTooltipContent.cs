using System.Collections.Generic;
using UnityEngine;

// 보드 칸(Button) 정보창의 내용 — 칸 자체의 상태와 칸 위 기물의 상태를 함께 담는다. 비어 있으면 정보창을 띄우지 않는다.
// 기물: 스탯 요약 → 상태이상·버프(효과마다 창 하나) → 다음 행동(적·자동 아군, 카드 이미지) → 범위 격자.
public static class BoardTooltipContent
{
    public static List<TooltipEntry> Build(Button button)
    {
        var result = new List<TooltipEntry>();
        if (button == null) return result;
        AddTileState(button, result);
        Piece piece = button.GetPieceScript();
        if (piece != null) AddPiece(piece, result);
        return result;
    }

    // 값과 기본값의 차이를 괄호 안에 색으로 붙인다 — 오르면 파랑, 내리면 빨강(카드 설명 수치와 같은 색). 기물 정보창(ButtonInfo)도 쓴다.
    public static string ValueWithDelta(int value, int baseValue)
    {
        int diff = value - baseValue;
        if (diff == 0) return value.ToString();
        string color = diff < 0 ? "#FF4444" : "#4444FF";
        string sign = diff < 0 ? "-" : "+";
        return $"{value} <color={color}>({sign}{Mathf.Abs(diff)})</color>";
    }

    // ── 칸 ──────────────────────────────────────────────────

    // 칸 자체의 상태(장판 등). 아직 칸 상태가 구현되지 않아 아무것도 넣지 않는다 — 구현되면 여기서 창을 추가한다.
    static void AddTileState(Button button, List<TooltipEntry> result) { }

    // ── 기물 ────────────────────────────────────────────────

    static void AddPiece(Piece piece, List<TooltipEntry> result)
    {
        result.Add(BuildStats(piece));
        // 모닥불·상점·NPC 같은 이벤트 오브젝트는 안내만 보여준다.
        if (piece.teamID != 0 && piece.teamID != 1) return;

        AddStatuses(piece, result);

        Card nextMove = (piece as AutoPiece)?.GetNextMove();
        if (nextMove != null)
        {
            result.Add(new TooltipEntry { kind = TooltipEntry.Kind.Action, title = "다음 행동", cardFace = nextMove });
            AddActionKeywords(piece, nextMove, result);
        }

        AddRanges(piece, nextMove, result);
    }

    static TooltipEntry BuildStats(Piece p)
    {
        switch (p)
        {
            case RestObject rest:
            {
                string heal = rest.healAmount < 0 ? "아군 전원을 최대 체력까지 회복" : $"아군 전원의 체력을 {rest.healAmount} 회복";
                string upgrade = $"아군 {rest.upgradeSelectCount}명의 힘을 영구히 {rest.upgradeColDamageBonus} 올림";
                if (rest.upgradeShieldBonusBonus > 0) upgrade += $"(방어막 보너스 +{rest.upgradeShieldBonusBonus})";
                string body = rest.used
                    ? "이미 사용했습니다."
                    : $"둘 중 하나를 고를 수 있습니다.\n· 휴식: {heal}\n· 강화: {upgrade}";
                return Stats("휴식 지점", body);
            }
            case ShopObject _:
                return Stats("상점", "카드와 유물을 사거나 덱의 카드를 제거할 수 있습니다.");
            case NPC npc:
                return Stats(npc.name, "대화할 수 있습니다.");
        }

        var lines = new List<string>
        {
            (p.teamID == 0 ? "아군" : "적") + (p.isSummon ? " 소환수" : ""),
            $"체력 {p.hp}/{p.maxhp}",
            $"방어도 {p.shield}",
            $"힘 {ValueWithDelta(p.colDamage, p.baseColDamage)}",
            $"방어막 보너스 {ValueWithDelta(p.shieldBonus, p.baseShieldBonus)}",
        };
        // 무덤은 아군 카드만 쓰므로 아군에게만 보여준다(적의 무덤은 의미 없음 — TC-GRAVE-003).
        if (p.teamID == 0) lines.Add($"무덤 {p.grave}");
        if (p.summonColDamagePending > 0 || p.summonMaxHpPending > 0)
            lines.Add($"다음 소환: 힘 +{p.summonColDamagePending}, 체력 +{p.summonMaxHpPending}");
        return Stats(p.name, string.Join("\n", lines));
    }

    static TooltipEntry Stats(string title, string body) =>
        new TooltipEntry { kind = TooltipEntry.Kind.Stats, title = title, body = body };

    // 걸린 상태이상·버프마다 창 하나 — 남은 턴 + 설명. 범위가 있는 턴 효과(FlameThrowingCard·WardZoneCard 등)는
    // 그 범위 격자도 같은 창에 그린다 — 발동할 때 효과를 가진 기물의 위치가 중심이다(Board.TurnEffects).
    static void AddStatuses(Piece p, List<TooltipEntry> result)
    {
        foreach (StatusEffect e in p.activeEffects)
        {
            string duration = e.duration >= 0 ? $"{e.duration}턴 남음"
                : e is NextMoveAttackEffect ? "이동공격할 때까지" : "영구";
            string desc = e switch
            {
                TurnEffect _          => null, // 이름("턴 종료 시 광역 피해 3" 등)이 곧 설명
                NextMoveAttackEffect _ => "다음 이동공격 때 소모됩니다.",
                _                      => KeywordGlossary.Get(GlossaryTermOf(e))?.description,
            };
            result.Add(new TooltipEntry
            {
                kind = e.IsBuff ? TooltipEntry.Kind.Buff : TooltipEntry.Kind.Debuff,
                titleColor = e.IsBuff ? (Color?)null : e.EffectColor, // 디버프는 상태 텍스트와 같은 효과별 색
                title = e.DisplayName,
                body = desc == null ? duration : $"{duration}\n{desc}",
                grid = TurnEffectRange(e),
            });
        }

        // 연쇄 이동공격은 StatusEffect가 아니라 컴포넌트라 상태이상 목록에 없다(ChainMoveAttackBuff 참고).
        if (p.GetComponent<ChainMoveAttackBuff>() != null)
            result.Add(new TooltipEntry
            {
                kind = TooltipEntry.Kind.Buff,
                title = "연쇄 이동공격",
                body = "이번 전투 동안\n이동공격 시 이동 범위 내 다른 적에게 한 번 더 같은 피해를 줍니다. 다른 적이 없으면 공격한 적에게 줍니다.",
            });
    }

    // 다음 행동 카드에 나오는 용어의 설명 창(손패 카드 정보창과 같은 규칙). 이 기물에 이미 걸려 있어 상태 창이
    // 같은 설명을 보여주는 용어(예: 기절한 적의 StunnedCard → "기절")는 건너뛴다.
    static void AddActionKeywords(Piece piece, Card nextMove, List<TooltipEntry> result)
    {
        var shownTerms = new HashSet<string>();
        foreach (StatusEffect e in piece.activeEffects)
        {
            string term = GlossaryTermOf(e);
            if (term != null) shownTerms.Add(term);
        }
        foreach (TooltipEntry entry in CardTooltipContent.BuildKeywords(nextMove))
            if (!shownTerms.Contains(entry.title))
                result.Add(entry);
    }

    static RangeGrid TurnEffectRange(StatusEffect e)
    {
        CardEffect ce = (e as TurnEffect)?.cardEffect;
        if (ce == null || ce.effectRange == null || !CardTooltipContent.IsAreaLogic(ce.targetlogic)) return null;
        return RangeGrid.FromOffsets(ce.effectRange.GetAbleRange(), RangeGrid.Cell.Caster);
    }

    static string GlossaryTermOf(StatusEffect e) => e switch
    {
        PoisonEffect _           => "독",
        BurningEffect _          => "화상",
        RegenEffect _            => "재생",
        StunEffect _             => "기절",
        StrengthenEffect _       => "강화",
        WeakenEffect _           => "약화",
        ThornEffect _            => "가시",
        VulnerableEffect _       => "취약",
        TauntEffect _            => "도발",
        MovementDisabledEffect _ => "이동 불가",
        ExileOnUseEffect _       => "소멸 저주",
        BondEffect _             => "결속",
        WoundOnHitEffect _       => "상처 입히기",
        _                        => null,
    };

    // 이동 범위(적·자동 아군은 다음 행동의 범위로 대신 — 이동할 차례면 그게 이동 범위다) + 이동공격 스플래시 범위.
    static void AddRanges(Piece p, Card nextMove, List<TooltipEntry> result)
    {
        if (p is AutoPiece auto)
        {
            if (nextMove != null)
                foreach (TooltipEntry entry in CardTooltipContent.BuildRanges(nextMove, auto))
                {
                    entry.title = $"다음 행동 {entry.title}";
                    result.Add(entry);
                }
        }
        else
        {
            result.Add(new TooltipEntry
            {
                kind = TooltipEntry.Kind.Range, title = "이동 범위",
                grid = RangeGrid.FromOffsets(p.GetMoveableButton(), RangeGrid.Cell.Caster),
            });
        }

        // 중앙 1칸뿐이면(설정 없음) 부딪힌 대상만 때리는 기본 이동공격이라 따로 보여줄 범위가 없다.
        List<Vector2Int> splash = p.GetMoveAttackRange();
        if (splash.Count == 1 && splash[0] == Vector2Int.zero) return;
        result.Add(new TooltipEntry
        {
            kind = TooltipEntry.Kind.Range, title = "이동공격 범위",
            grid = RangeGrid.FromOffsets(splash, RangeGrid.Cell.TargetPoint),
        });
    }
}
