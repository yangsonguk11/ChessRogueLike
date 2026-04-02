using System.Collections;
using TMPro;
using UnityEngine;

public class PieceCanvas : MonoBehaviour
{
    [SerializeField] GameObject DamageText;
    public float duration = 1.0f;           // 표시 시간
    [SerializeField] float moveSpeed;         // 위로 올라가는 속도

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
        textobj.GetComponent<TextMeshProUGUI>().text = text;
        textobj.transform.localPosition = new Vector3(0, 3f, 0);
        currentText = textobj;

    }
    public void InvokeDamageText(int dmg)
    {
        GameObject textobj = Instantiate(DamageText, transform);
        StartCoroutine(DamageCoroutine(dmg, textobj));
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
