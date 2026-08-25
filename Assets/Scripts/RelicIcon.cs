using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 유물 아이콘 프리팹에 붙는 컴포넌트. ISelectable을 구현해 NodeButton/Card와 동일한 호버 스케일
// 피드백을 재사용하고, 커서를 올리면 이름/설명 텍스트를 보여준다. Board가 유물 효과를 실제로
// 적용하는 부분(Relic 서브클래스, RelicDatabase.CreateRelic, Board.Relics.cs)과는 완전히 분리돼
// 있다 — 여기서는 표시용 이름/설명만 그때그때 읽어올 뿐, CardEffect는 전혀 모른다.
//
// EventTrigger 자동 부착 + PointerEnter/PointerExit 등록은 ISelectable.RegisterEventTrigger()가
// 담당한다(Awake()에서 한 번 호출) — 프리팹마다 인스펙터에서 손으로 이벤트를 연결할 필요가 없다.
[RequireComponent(typeof(EventTrigger))]
public class RelicIcon : MonoBehaviour, ISelectable
{
    // RelicDatabase.SpawnIcon이 스폰 직후 채워주는 식별자(Relic 서브클래스 이름). 이 값으로
    // RelicDatabase.CreateRelic을 호출해 이름/설명만 읽어오고, 그 인스턴스는 바로 버린다.
    public string relicName;

    [SerializeField] TextMeshProUGUI tooltipText;   // 호버 시 내용을 채울 텍스트
    [SerializeField] GameObject tooltipObject;      // tooltipText를 포함하는 오브젝트(배경 패널 등). 기본 비활성.

    // 클릭 시 문맥(상점 구매 등)에 따라 호출부가 주입한다(Card.onClickOverride와 동일한 역할).
    public System.Action<string> onClickOverride;

    string displayName, description;

    bool _selected;
    public bool selected
    {
        get => _selected;
        set => _selected = value;
    }

    Vector3 defaultScale;
    float hoverScale = 1.1f;
    float speed = 10f;

    void Awake()
    {
        defaultScale = transform.localScale;
        tooltipObject?.SetActive(false);
        ((ISelectable)this).RegisterEventTrigger(); // ISelectable의 공용 등록 로직 재사용(PointerEnter/Exit)

        // 클릭(PointerClick)은 ISelectable에 없는 RelicIcon 전용 동작이라 여기서 직접 등록한다.
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(_ => onClickOverride?.Invoke(relicName));
        trigger.triggers.Add(clickEntry);
    }

    void Start()
    {
        Relic data = RelicDatabase.instance?.CreateRelic(relicName);
        displayName = data != null ? data.Name : relicName;
        description = data != null ? data.Description : "";
    }

    public bool IsSelectable() => true;

    public IEnumerator ScaleTo(Vector3 target) => ScaleAnimator.ScaleTo(transform, target, speed);
    public void ScaleDefault()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale));
    }
    public void ScaleHover()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(defaultScale * hoverScale));
    }

    public void MouseEnter()
    {
        ScaleHover();
        if (tooltipText != null)
            tooltipText.text = string.IsNullOrEmpty(description) ? displayName : $"{displayName}\n{description}";

        if (tooltipObject == null) return;
        // ContentSizeFitter/LayoutGroup은 비활성 상태에선 계산을 건너뛰므로, 먼저 켠 뒤 같은 프레임에
        // 강제로 레이아웃을 다시 계산시켜야 프리팹 기본 텍스트("New Text") 크기로 잠깐 보이는 깜빡임이 없다.
        tooltipObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipObject.GetComponent<RectTransform>());
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
        tooltipObject?.SetActive(false);
    }
}
