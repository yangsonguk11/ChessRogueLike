using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PieceGaugeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI piecename;
    [SerializeField] TextMeshProUGUI team;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI shield;
    [SerializeField] TextMeshProUGUI hand;
    [SerializeField] Image hpFillImage;

    public Piece Piece { get; private set; }

    public void Bind(Piece piece)
    {
        Piece = piece;
    }

    public void Refresh()
    {
        if (Piece == null) return;

        piecename.text = Piece.name;
        team.text = Piece.teamID == 0 ? "아군" : "적";
        hp.text = string.Format("HP: {0}/{1}", Piece.hp, Piece.maxhp);
        shield.text = "방어막: " + Piece.shield;

        if (hand != null)
        {
            bool hasDeck = Piece.pieceDeck != null;
            hand.gameObject.SetActive(hasDeck);
            if (hasDeck)
                hand.text = string.Format("손패: {0}/{1}", Piece.pieceDeck.Hand.Count, Piece.pieceDeck.Deck.Count);
        }

        if (hpFillImage != null)
            hpFillImage.fillAmount = Piece.maxhp > 0 ? Mathf.Clamp01((float)Piece.hp / Piece.maxhp) : 0f;
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
