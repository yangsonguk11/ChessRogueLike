using System.Collections;
using TMPro;
using UnityEngine;

public class PieceCanvas : MonoBehaviour
{
    [SerializeField] GameObject DamageText;
    public float duration = 1.0f;             // 텍스트 표시 시간
    [SerializeField] float moveSpeed;         // 텍스트가 위로 올라가는 속도
    [SerializeField] Color buffColor = new Color(0f, 1f, 0.53f);    // #00FF88
    [SerializeField] Color debuffColor = new Color(1f, 0.27f, 0.27f); // #FF4444

    GameObject currentText;

    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,Camera.main.transform.rotation * Vector3.up);
    }
    
    public void ShowActionText(string text)
    {
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
        StartCoroutine(FloatAndFadeCoroutine(textobj));
    }

    // 버프/디버프가 적용될 때 어떤 효과인지(예: "독 (2/턴)") 데미지 텍스트와 같은 자리에 띄움
    public void InvokeStatusText(string text, bool isBuff)
    {
        GameObject textobj = Instantiate(DamageText, transform);
        TextMeshProUGUI tmp = textobj.GetComponent<TextMeshProUGUI>();
        BringToFrontOfRangeHatch(tmp);
        tmp.text = text;
        tmp.color = isBuff ? buffColor : debuffColor;
        StartCoroutine(FloatAndFadeCoroutine(textobj));
    }

    // RangeHatch 셰이더가 Queue=Transparent+1로 그려져서 기본 Transparent(3000) 큐인
    // 텍스트/스프라이트를 가려버리는 문제 보정. 인스턴스 머티리얼 큐를 그보다 높여서
    // 항상 RangeHatch 위에 그려지도록 한다.
    void BringToFrontOfRangeHatch(TextMeshProUGUI tmp)
    {
        foreach (Material mat in tmp.fontMaterials)
            mat.renderQueue = 3002;
    }
    IEnumerator FloatAndFadeCoroutine(GameObject textobj)
    {
        TextMeshProUGUI text = textobj.GetComponent<TextMeshProUGUI>();
        Color baseColor = text.color;

        Vector3 startPos = new Vector3(0, 2.0f, 0);
        float time = 0f;

        Vector3 v = new Vector3(0, moveSpeed, 0);
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            textobj.transform.localPosition = startPos + (v * t);
            Color c = baseColor;
            c.a = baseColor.a * (1f - t);
            text.color = c;
            yield return null;
        }

        Destroy(textobj);
    }

}
