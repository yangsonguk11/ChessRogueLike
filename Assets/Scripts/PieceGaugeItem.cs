using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PieceGaugeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const string BuffColor = "#5A6BE0"; // 정보창·카드 설명의 증가 색과 같은 파랑

    [SerializeField] TextMeshProUGUI piecename;
    [SerializeField] TextMeshProUGUI team;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI shield;
    [SerializeField] TextMeshProUGUI hand;
    [SerializeField] TextMeshProUGUI shieldBonus;
    [SerializeField] TextMeshProUGUI grave;
    [SerializeField] TextMeshProUGUI summonBonus;
    [SerializeField] TextMeshProUGUI statuses; // Rich Text 필요(효과별 색)
    [SerializeField] Image hpFillImage;

    public Piece Piece { get; private set; }

    readonly List<string> statusParts = new List<string>();

    public void Bind(Piece piece)
    {
        Piece = piece;
    }

    public void Refresh()
    {
        if (Piece == null) return;

        piecename.text = Piece.name;
        team.text = (Piece.teamID == 0 ? "아군" : "적") + (Piece.isSummon ? " 소환수" : "");
        hp.text = string.Format("HP: {0}/{1}", Piece.hp, Piece.maxhp);
        shield.text = "방어막: " + Piece.shield;

        bool hasDeck = Piece.pieceDeck != null;
        SetLine(hand, hasDeck, hasDeck
            ? string.Format("손패: {0}/{1}", Piece.pieceDeck.Hand.Count, Piece.pieceDeck.Deck.Count)
            : null);

        // 모닥불·상점·NPC 같은 이벤트 오브젝트는 정보창처럼 전투 정보를 숨긴다.
        bool combatant = Piece.teamID == 0 || Piece.teamID == 1;
        SetLine(shieldBonus, combatant,
            "방어막 보너스 " + BoardTooltipContent.ValueWithDelta(Piece.shieldBonus, Piece.baseShieldBonus));
        // 무덤은 아군 카드만 쓰므로 아군에게만 보여준다(정보창과 같은 규칙 — TC-GRAVE-003).
        SetLine(grave, Piece.teamID == 0, "무덤 " + Piece.grave);
        bool hasSummonBonus = combatant && (Piece.summonColDamagePending > 0 || Piece.summonMaxHpPending > 0);
        SetLine(summonBonus, hasSummonBonus,
            $"다음 소환: 힘 +{Piece.summonColDamagePending}, 체력 +{Piece.summonMaxHpPending}");

        if (statuses != null)
        {
            string summary = combatant ? BuildStatusSummary(Piece) : null;
            SetLine(statuses, !string.IsNullOrEmpty(summary), summary);
        }

        if (hpFillImage != null)
            hpFillImage.fillAmount = Piece.maxhp > 0 ? Mathf.Clamp01((float)Piece.hp / Piece.maxhp) : 0f;
    }

    static void SetLine(TextMeshProUGUI label, bool visible, string text)
    {
        if (label == null) return;
        label.gameObject.SetActive(visible);
        if (visible) label.text = text;
    }

    // 걸린 상태이상·버프를 한 줄로 — 이름 + 남은 턴(정보창 표기의 축약형). 디버프는 효과별 색, 버프는 파랑.
    string BuildStatusSummary(Piece p)
    {
        statusParts.Clear();
        foreach (StatusEffect e in p.activeEffects)
        {
            string color = e.IsBuff ? BuffColor : "#" + ColorUtility.ToHtmlStringRGB(e.EffectColor);
            statusParts.Add(e.StatusLabel(color));
        }
        // 연쇄 이동공격은 StatusEffect가 아니라 컴포넌트라 상태이상 목록에 없다(ChainMoveAttackBuff 참고).
        if (p.GetComponent<ChainMoveAttackBuff>() != null)
            statusParts.Add($"<color={BuffColor}>연쇄 이동공격</color>");
        return string.Join(" · ", statusParts);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Board.instance?.HoverPieceFromUI(Piece);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Board.instance?.UnhoverPieceFromUI(Piece);
    }
}
