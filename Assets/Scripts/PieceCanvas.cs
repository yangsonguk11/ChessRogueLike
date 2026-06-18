using System.Collections;
using TMPro;
using UnityEngine;

public class PieceCanvas : MonoBehaviour
{
    [SerializeField] GameObject DamageText;
    public float duration = 1.0f;           // ǥ�� �ð�
    [SerializeField] float moveSpeed;         // ���� �ö󰡴� �ӵ�

    GameObject currentText;
    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,Camera.main.transform.rotation * Vector3.up);
    }
    
    public void ShowActionText(string text)
    {
        Destroy(currentText);
        Debug.Log(text);
        GameObject textobj = Instantiate(DamageText, transform);
        TextMeshProUGUI tmp = textobj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        textobj.transform.localPosition = new Vector3(0, 3f, 0);
        tmp.ForceMeshUpdate(); // sprite 서브메시(아이콘 머티리얼)가 생성되어야 fontMaterials에 잡힌다
        BringToFrontOfRangeHatch(tmp);
        currentText = textobj;

    }
    public void InvokeDamageText(int dmg)
    {
        GameObject textobj = Instantiate(DamageText, transform);
        BringToFrontOfRangeHatch(textobj.GetComponent<TextMeshProUGUI>());
        StartCoroutine(DamageCoroutine(dmg, textobj));
    }

    // RangeHatch 셰이더가 Queue=Transparent+1로 그려져서 기본 Transparent(3000) 큐인
    // 텍스트/스프라이트를 가려버리는 문제 보정. 인스턴스 머티리얼 큐를 그보다 높여서
    // 항상 RangeHatch 위에 그려지도록 한다.
    void BringToFrontOfRangeHatch(TextMeshProUGUI tmp)
    {
        foreach (Material mat in tmp.fontMaterials)
            mat.renderQueue = 3002;
    }
    IEnumerator DamageCoroutine(int dmg, GameObject textobj)
    {
        TextMeshProUGUI text = textobj.GetComponent<TextMeshProUGUI>();
        text.text = dmg.ToString();

        Vector3 startPos = new Vector3(0, 2.0f, 0);
        float time = 0f;

        Vector3 v = new Vector3(0, moveSpeed, 0);
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            textobj.transform.localPosition = startPos + (v * t);
            Color c = text.color;
            c.a = 1f - t;
            text.color = c;
            yield return null;
        }

        Destroy(textobj);
    }

}
