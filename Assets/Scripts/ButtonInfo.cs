using System.Text;
using TMPro;
using UnityEngine;

public class ButtonInfo : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI piecename;
    [SerializeField] TextMeshProUGUI team;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI shield;
    [SerializeField] TextMeshProUGUI colDamage;
    [SerializeField] TextMeshProUGUI statusEffects;
    [SerializeField] UnityEngine.UI.Button restButton;

    public void UpdateButtonInfo(Button button)
    {
        Piece p = button.GetPiece().GetComponent<Piece>();

        if (p is RestObject restObject)
        {
            piecename.text = "휴식 지점";
            team.text = "";
            hp.text = "";
            shield.text = "";
            colDamage.text = "";
            statusEffects.text = "";
            SetRestButtonActive(!restObject.used);
            return;
        }

        SetRestButtonActive(false);
        piecename.text = p.name;
        team.text = p.teamID == 0 ? "아군" : "적";
        hp.text = string.Format("HP: {0}/{1}", p.hp, p.maxhp);
        shield.text = "방어막: " + p.shield;
        colDamage.text = "충돌 피해: " + BuildColDamageText(p);
        statusEffects.text = BuildStatusText(p);
    }

    void SetRestButtonActive(bool active)
    {
        if (restButton == null) return;
        restButton.gameObject.SetActive(active);
        restButton.onClick.RemoveAllListeners();
        if (active)
            restButton.onClick.AddListener(() => Board.instance.RestHeal());
    }

    string BuildColDamageText(Piece p)
    {
        int diff = p.colDamage - p.baseColDamage;
        if (diff == 0)
            return p.colDamage.ToString();

        string color = diff < 0 ? "#FF4444" : "#4444FF";
        string sign = diff < 0 ? "-" : "+";
        return $"{p.colDamage} <color={color}>({sign}{Mathf.Abs(diff)})</color>";
    }

    string BuildStatusText(Piece p)
    {
        if (p.activeEffects == null || p.activeEffects.Count == 0)
            return "상태 이상: 없음";

        var sb = new StringBuilder();
        foreach (var effect in p.activeEffects)
        {
            string color = effect.IsBuff ? "#00FF88" : "#FF4444";
            sb.AppendLine($"<color={color}>{effect.DisplayName}  {effect.duration}턴</color>");
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
        colDamage.text = "";
        statusEffects.text = "";
        SetRestButtonActive(false);
    }

    void Start()
    {
        Clear();
    }
}
