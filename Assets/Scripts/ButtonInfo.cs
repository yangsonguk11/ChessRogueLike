using System.Text;
using TMPro;
using UnityEngine;

public class ButtonInfo : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI piecename;
    [SerializeField] TextMeshProUGUI team;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI shield;
    [SerializeField] TextMeshProUGUI shieldBonus;
    [SerializeField] TextMeshProUGUI colDamage;
    [SerializeField] TextMeshProUGUI statusEffects;
    [SerializeField] UnityEngine.UI.Button restButton;
    [SerializeField] UnityEngine.UI.Button upgradeButton;

    public void UpdateButtonInfo(Button button)
    {
        GameObject pieceObj = button.GetPiece();
        if (pieceObj == null)
        {
            Clear();
            return;
        }

        Piece p = pieceObj.GetComponent<Piece>();

        if (p is RestObject restObject)
        {
            piecename.text = "휴식 지점";
            team.text = "";
            hp.text = "";
            shield.text = "";
            shieldBonus.text = "";
            colDamage.text = "";
            statusEffects.text = "";
            SetRestActionsActive(!restObject.used);
            return;
        }

        if (p is ShopObject)
        {
            piecename.text = "상점";
            team.text = "";
            hp.text = "";
            shield.text = "";
            shieldBonus.text = "";
            colDamage.text = "";
            statusEffects.text = "";
            SetRestActionsActive(false);
            return;
        }

        SetRestActionsActive(false);
        piecename.text = p.name;
        team.text = p.teamID == 0 ? "아군" : "적";
        hp.text = string.Format("HP: {0}/{1}", p.hp, p.maxhp);
        shield.text = "방어막: " + p.shield;
        shieldBonus.text = "방어막 보너스: " + BuildShieldBonusText(p);
        colDamage.text = "힘: " + BuildColDamageText(p);
        statusEffects.text = BuildStatusText(p);
    }

    void SetRestActionsActive(bool active)
    {
        if (restButton != null)
        {
            restButton.gameObject.SetActive(active);
            restButton.onClick.RemoveAllListeners();
            if (active)
                restButton.onClick.AddListener(() => Board.instance.RestHeal());
        }

        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(active);
            upgradeButton.onClick.RemoveAllListeners();
            if (active)
                upgradeButton.onClick.AddListener(() => Board.instance.RestUpgrade());
        }
    }

    string BuildColDamageText(Piece p) => BoardTooltipContent.ValueWithDelta(p.colDamage, p.baseColDamage);

    string BuildShieldBonusText(Piece p) => BoardTooltipContent.ValueWithDelta(p.shieldBonus, p.baseShieldBonus);

    // 보드 칸 정보창(BoardTooltipHost)이 같은 한글 폰트를 쓰도록 넘겨준다 — TMP 기본 폰트엔 한글이 없다.
    public TMP_FontAsset Font => piecename != null ? piecename.font : null;

    string BuildStatusText(Piece p)
    {
        if (p.activeEffects == null || p.activeEffects.Count == 0)
            return "상태 이상: 없음";

        var sb = new StringBuilder();
        foreach (var effect in p.activeEffects)
        {
            string color = effect.IsBuff ? "#00FF88" : "#FF4444";
            string durationSuffix = effect.duration < 0 ? "" : $"  {effect.duration}턴"; // 영구(음수)면 턴 표시 생략
            sb.AppendLine($"<color={color}>{effect.DisplayName}{durationSuffix}</color>");
        }
        return sb.ToString().TrimEnd();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Clear()
    {
        piecename.text = "";
        team.text = "";
        hp.text = "";
        shield.text = "";
        shieldBonus.text = "";
        colDamage.text = "";
        statusEffects.text = "";
        SetRestActionsActive(false);
    }

    void Start()
    {
        Clear();
    }
}
