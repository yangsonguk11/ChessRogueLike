using TMPro;
using UnityEngine;

public class BoardUICanvas : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI piecename;
    [SerializeField] TextMeshProUGUI hp;
    [SerializeField] TextMeshProUGUI isAlly;


    public void UpdateButtonInfo(Button button)
    {
        Piece p = button.GetPiece().GetComponent<Piece>();
        piecename.text = p.name;
        hp.text = string.Format("{0}/{1}", p.hp, p.maxhp);
        isAlly.text = p.teamID == 0 ? "Ally" : "Enemy";
    }


    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
