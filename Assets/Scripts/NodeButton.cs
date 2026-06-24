using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NodeButton : MonoBehaviour, ISelectable
{
    public bool selected { get; set; }
    public MapNode nodeData;
    public int nodeFloor;
    public bool selectable;

    [SerializeField] Image nodeImage;
    [SerializeField] Image iconImage;

    public bool IsSelectable() => selectable;

    void Awake()
    {
        defaultScale = transform.localScale;
        if (nodeImage == null)
            nodeImage = GetComponent<Image>();
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void SetVisualState(bool isSelectable, bool isCurrent, bool isVisited = false)
    {
        if (nodeImage == null) return;
        if (isCurrent)
            nodeImage.color = new Color(1f, 0.8f, 0f);        // 금색: 현재 위치
        else if (isVisited)
            nodeImage.color = new Color(0.4f, 0.9f, 0.4f);   // 녹색: 방문함
        else if (isSelectable)
            nodeImage.color = Color.white;                     // 흰색: 선택 가능
        else
            nodeImage.color = new Color(0.35f, 0.35f, 0.35f); // 회색: 선택 불가
    }

    Vector3 defaultScale;
    float hoverScale = 1.15f;
    float speed = 10f;

    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);

    public void ScaleDefault()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale));
    }

    public void ScaleHover()
    {
        if (!selectable) return;
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }

    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
    }

    public void MouseDown()
    {
        if (!selectable) return;
        DataManager.Instance.SetNextLevel(nodeData.levelDataName, nodeFloor, nodeData.x);
        SceneManager.LoadScene("MainScene");
    }
}
