using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 정보창(TooltipView) 한 칸(창 하나)의 내용. grid가 있으면 범위 격자를, cardFace가 있으면 그 카드의 이미지를 그린다.
public class TooltipEntry
{
    // 제목 색을 정한다 — 범위·용어(카드 정보창), 스탯·버프·디버프·다음 행동(보드 칸 정보창).
    public enum Kind { Range, Keyword, Stats, Buff, Debuff, Action }

    public Kind kind;
    public string title;
    public Color? titleColor; // 지정하면 kind의 기본 제목 색 대신 쓴다(디버프의 효과별 색 등)
    public string body;
    public RangeGrid grid;
    public Card cardFace;
}

// 범위 격자. cells[row, col] — row 0이 위, col 0이 왼쪽(RangeInfoSO 인스펙터 배치와 같은 방향).
public class RangeGrid
{
    public enum Cell { Empty, Caster, InRange, TargetPoint }

    public readonly Cell[,] cells;
    public int Size => cells.GetLength(0);

    RangeGrid(int size) { cells = new Cell[size, size]; }

    // offsets는 RangeInfoSO.GetAbleRange()와 같은 좌표계(x = 행, y = 열, 중심 0). 크기는 가장 먼 오프셋까지만
    // 잘라 컴팩트하게 만들고(최소 3×3), 중심 칸은 center로 칠한다.
    public static RangeGrid FromOffsets(IEnumerable<Vector2Int> offsets, Cell center)
    {
        List<Vector2Int> list = offsets.ToList();
        int radius = Mathf.Max(1, list.Count == 0 ? 0 : list.Max(o => Mathf.Max(Mathf.Abs(o.x), Mathf.Abs(o.y))));
        var grid = new RangeGrid(radius * 2 + 1);
        foreach (Vector2Int o in list)
            grid.cells[o.x + radius, o.y + radius] = Cell.InRange;
        grid.cells[radius, radius] = center;
        return grid;
    }
}

// 카드 하나의 정보창 목록을 만든다 — 범위 창들 먼저, 용어 설명 창들 다음.
// 범위 판정은 실제 보드 하이라이트(Board.ShowCasterEffectRange, Board.HoverRange)와 같은 분기를 따른다.
public static class CardTooltipContent
{
    public static List<TooltipEntry> Build(Card card)
    {
        var result = new List<TooltipEntry>();
        // 기물 이동 범위로 대상을 고르는 효과는 이 카드를 가진 기물을 알 수 있을 때(손패·카드 선택 패널)만 그 기물 기준 격자로
        // 보여 주고, 덱·버림·소멸 보기·상점·보상처럼 모를 땐 범위 창을 띄우지 않는다(용어 창 "이동"이 설명).
        Piece owner = (CardCanvas.cardSelectionMode || card.IsSelectable()) ? CardCanvas.instance?.ActivePiece : null;
        AddRanges(card, owner, result);
        AddKeywords(card, result);
        return result;
    }

    // card의 범위 창들만 — 보드 칸 정보창이 적·자동 아군의 다음 행동 범위를 보여줄 때 쓴다(owner = 그 기물).
    public static List<TooltipEntry> BuildRanges(Card card, Piece owner)
    {
        var result = new List<TooltipEntry>();
        AddRanges(card, owner, result);
        return result;
    }

    // ── 범위 ────────────────────────────────────────────────

    static void AddRanges(Card card, Piece owner, List<TooltipEntry> result)
    {
        var seen = new HashSet<(RangeInfoSO, string)>();
        bool moveRangeAdded = false;

        void AddGrid(string title, RangeInfoSO so, RangeGrid.Cell center, string body = null)
        {
            if (so == null || !seen.Add((so, title))) return;
            result.Add(new TooltipEntry
            {
                kind = TooltipEntry.Kind.Range, title = title, body = body,
                grid = RangeGrid.FromOffsets(so.GetAbleRange(), center),
            });
        }

        void AddText(string title, string body)
        {
            if (!seen.Add((null, title))) return;
            result.Add(new TooltipEntry { kind = TooltipEntry.Kind.Range, title = title, body = body });
        }

        // 기물 이동 범위로 대상을 고르는 효과(이동, 범위 없는 대상 지정, targetingUsesMovement).
        // owner(카드를 가진 기물)를 알 때만 그 기물 기준 격자로 보여 준다. 모르면(덱·버림·소멸 보기, 상점, 보상) 격자 없는
        // 빈 창이 되므로 띄우지 않는다 — 이동 효과는 용어 창 "이동"이 설명한다.
        void AddMoveRange(string title)
        {
            if (moveRangeAdded || owner == null) return;
            moveRangeAdded = true;
            result.Add(new TooltipEntry
            {
                kind = TooltipEntry.Kind.Range, title = title,
                grid = RangeGrid.FromOffsets(owner.GetMoveableButton(), RangeGrid.Cell.Caster),
            });
        }

        foreach (CardEffect effect in card.effects)
        {
            if (effect == null || effect.useLastTarget || effect.pieceSelectCount > 0) continue;

            // 턴 효과는 감싼 효과의 범위(시전자 주변)를 보여 준다.
            if (effect.type == EffectType.ApplyTurnEffect)
            {
                CardEffect inner = effect.onTurnEndEffect;
                if (inner != null && inner.effectRange != null && IsAreaLogic(inner.targetlogic))
                    AddGrid("효과 범위 (턴 효과)", inner.effectRange, RangeGrid.Cell.Caster);
                continue;
            }

            if (effect.targetlogic == TargetLogic.self && effect.type != EffectType.Move) continue;
            if (effect.requiredMode == Board.BoardMode.Inspect) continue;

            if (effect.areaTargetMode == AreaTargetMode.MouseCentered)
            {
                if (effect.noRangeLimit) AddText("사거리", "보드 전체");
                else if (effect.targetingRange != null) AddGrid("사거리", effect.targetingRange, RangeGrid.Cell.Caster);
                else if (effect.targetingUsesMovement) AddMoveRange("사거리");
                else AddText("사거리", "보드 전체");
                AddGrid("효과 범위", effect.effectRange, RangeGrid.Cell.TargetPoint, "지정한 칸을 중심으로 펼쳐집니다.");
                continue;
            }

            if (effect.noRangeLimit)
            {
                AddText("사거리", "보드 전체");
                continue;
            }

            if (effect.areaTargetMode == AreaTargetMode.Directional4 || effect.areaTargetMode == AreaTargetMode.Directional8)
            {
                string dirs = effect.areaTargetMode == AreaTargetMode.Directional4 ? "4방향" : "8방향";
                AddGrid("효과 범위", effect.effectRange, RangeGrid.Cell.Caster, $"{dirs} 중 고른 방향으로 회전합니다.");
                continue;
            }

            if (effect.type == EffectType.Move || effect.effectRange == null)
            {
                AddMoveRange(effect.type == EffectType.Move ? "이동 범위" : "사거리");
                continue;
            }

            if (effect.lockOnAllyPositions)
                AddGrid("효과 범위", effect.effectRange, RangeGrid.Cell.TargetPoint, "각 아군 위치를 중심으로 펼쳐집니다.");
            else if (IsAreaLogic(effect.targetlogic))
                AddGrid("효과 범위", effect.effectRange, RangeGrid.Cell.Caster);
            else if (effect.type == EffectType.Summon)
                AddGrid("소환 범위", effect.effectRange, RangeGrid.Cell.Caster);
            else
                AddGrid("사거리", effect.effectRange, RangeGrid.Cell.Caster);
        }
    }

    public static bool IsAreaLogic(TargetLogic logic) =>
        logic == TargetLogic.AllEnemiesInRange || logic == TargetLogic.AllAlliesInRange || logic == TargetLogic.AllPiecesInRange;

    // ── 용어 ────────────────────────────────────────────────

    // card의 용어 설명 창들만 — 보드 칸 정보창이 적·자동 아군의 다음 행동 카드 용어를 설명할 때 쓴다.
    public static List<TooltipEntry> BuildKeywords(Card card)
    {
        var result = new List<TooltipEntry>();
        AddKeywords(card, result);
        return result;
    }

    static void AddKeywords(Card card, List<TooltipEntry> result)
    {
        List<KeywordGlossary.Entry> found = KeywordGlossary.FindIn(card.EffectDescription);

        // 설명 텍스트에 안 나와도 카드 데이터상 분명한 용어는 덧붙인다.
        var extra = new List<string>(card.ExtraKeywords);
        var lead = new List<string>(); // 카드의 주 동작이라 맨 앞에 둘 용어(이동)
        if (card.ShowsExile) extra.Add("소멸");
        foreach (CardEffect effect in card.effects)
        {
            if (effect == null) continue;
            if (effect.type == EffectType.Move)
            {
                lead.Add("이동");
                if (!effect.noMoveAttack) lead.Add("이동공격"); // 이동공격 불가 이동(SafeMoveCard)에는 붙이지 않는다
            }
            if (effect.countsAsMoveAttack) extra.Add("이동공격 판정");
            if (effect.graveCost > 0 || effect.consumeAllGrave) extra.Add("무덤");
            AddStatusTerm(effect, extra);
            AddStatusTerm(effect.onMoveAttackHitEffect, extra);
            AddStatusTerm(effect.onTurnEndEffect, extra);
        }
        foreach (string term in extra)
        {
            KeywordGlossary.Entry entry = KeywordGlossary.Get(term);
            if (entry != null && !found.Contains(entry)) found.Add(entry);
        }
        // 맨 앞 용어는 텍스트에서 이미 찾았어도 그 자리에서 빼고 앞으로 옮긴다(중복 없음).
        int leadIndex = 0;
        foreach (string term in lead)
        {
            KeywordGlossary.Entry entry = KeywordGlossary.Get(term);
            if (entry == null) continue;
            int existing = found.IndexOf(entry);
            if (existing >= 0 && existing < leadIndex) continue; // 앞서 다른 이동 효과로 이미 맨 앞에 둠
            if (existing >= 0) found.RemoveAt(existing);
            found.Insert(leadIndex++, entry);
        }

        foreach (KeywordGlossary.Entry entry in found)
            result.Add(new TooltipEntry { kind = TooltipEntry.Kind.Keyword, title = entry.term, body = entry.description });
    }

    static void AddStatusTerm(CardEffect effect, List<string> terms)
    {
        string term = StatusTermOf(effect);
        if (term != null) terms.Add(term);
    }

    // 효과가 거는 상태이상의 용어(KeywordGlossary 대표 표기). 없으면 null — 보드 칸 정보창도 버프가 거는 상태의 용어 창에 쓴다.
    public static string StatusTermOf(CardEffect effect)
    {
        if (effect == null) return null;
        return effect.statusEffectType switch
        {
            StatusEffectType.Poison           => "독",
            StatusEffectType.Burning          => "화상",
            StatusEffectType.Regen            => "재생",
            StatusEffectType.Stun             => "기절",
            StatusEffectType.Strengthen       => "강화",
            StatusEffectType.Weaken           => "약화",
            StatusEffectType.Thorn            => "가시",
            StatusEffectType.MovementDisabled => "이동 불가",
            StatusEffectType.Vulnerable       => "취약",
            StatusEffectType.Taunt            => "도발",
            StatusEffectType.ExileOnUse       => "소멸 저주",
            StatusEffectType.WoundOnHit       => "상처 입히기",
            _ => null,
        };
    }
}
