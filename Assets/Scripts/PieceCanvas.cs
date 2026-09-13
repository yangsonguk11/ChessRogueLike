using DG.Tweening;
using TMPro;
using UnityEngine;

public class PieceCanvas : MonoBehaviour
{
    [SerializeField] GameObject DamageText;
    public float duration = 1.0f;             // 텍스트 표시 시간
    [SerializeField] float moveSpeed;         // 텍스트가 위로 올라가는 속도
    [SerializeField] Color buffColor = new Color(0f, 1f, 0.53f);    // #00FF88

    GameObject currentText;

    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,Camera.main.transform.rotation * Vector3.up);
    }
    
    public void ShowActionText(string text)
    {
        AudioManager.instance?.PlayEnemyTelegraph();
        Destroy(currentText);
        GameObject textobj = Instantiate(DamageText, transform);
        TextMeshProUGUI tmp = textobj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        textobj.transform.localPosition = new Vector3(0, 3f, 0);
        tmp.ForceMeshUpdate(); // sprite 서브메시(아이콘 머티리얼)가 생성되어야 fontMaterials에 잡힌다
        BringToFrontOfRangeHatch(tmp);
        currentText = textobj;

    }
    public void ClearActionText()
    {
        Destroy(currentText);
        currentText = null;
    }

    public void InvokeDamageText(int dmg)
    {
        GameObject textobj = Instantiate(DamageText, transform);
        BringToFrontOfRangeHatch(textobj.GetComponent<TextMeshProUGUI>());
        textobj.GetComponent<TextMeshProUGUI>().text = dmg.ToString();
        FloatAndFade(textobj);
    }

    // 버프/디버프가 적용될 때 어떤 효과인지(예: "독 (2/턴)") 데미지 텍스트와 같은 자리에 띄움
    // 디버프는 효과 종류별 색(effectColor)을 그대로 쓰고, 버프는 공통 buffColor를 쓴다.
    public void InvokeStatusText(string text, bool isBuff, Color effectColor)
    {
        GameObject textobj = Instantiate(DamageText, transform);
        TextMeshProUGUI tmp = textobj.GetComponent<TextMeshProUGUI>();
        BringToFrontOfRangeHatch(tmp);
        tmp.text = text;
        tmp.color = isBuff ? buffColor : effectColor;
        FloatAndFade(textobj);
    }

    // RangeHatch 셰이더가 Queue=Transparent+1로 그려져서 기본 Transparent(3000) 큐인
    // 텍스트/스프라이트를 가려버리는 문제 보정. 인스턴스 머티리얼 큐를 그보다 높여서
    // 항상 RangeHatch 위에 그려지도록 한다.
    void BringToFrontOfRangeHatch(TextMeshProUGUI tmp)
    {
        foreach (Material mat in tmp.fontMaterials)
            mat.renderQueue = 3002;
    }
    void FloatAndFade(GameObject textobj)
    {
        TextMeshProUGUI text = textobj.GetComponent<TextMeshProUGUI>();
        Color baseColor = text.color;
        Vector3 startPos = new Vector3(0, 2.0f, 0);
        textobj.transform.localPosition = startPos;

        textobj.transform.DOLocalMove(startPos + new Vector3(0, moveSpeed, 0) * duration, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => Destroy(textobj));

        DOTween.To(() => text.color.a,
            a => { Color c = baseColor; c.a = a; text.color = c; },
            0f, duration).SetEase(Ease.Linear);
    }

}
