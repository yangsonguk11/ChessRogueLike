using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 기물별 고유 덱 도입에 따라 "이 카드를 어느 기물에게 줄지" 골라야 하는 화면에서 공용으로 쓰는 선택 UI.
// 보드에 기물이 스폰돼 있지 않은 맵/보상 화면(ResultCanvas)과, 스폰돼 있는 전투 중(DialogueUI) 양쪽에서
// 동일한 방식(이름 목록 버튼)으로 동작해야 하므로 보드 클릭이 아니라 PieceData 목록 기반으로 만든다.
public class PieceTargetPickerUI : MonoBehaviour
{
    public static PieceTargetPickerUI instance;

    [SerializeField] GameObject panelRoot; // 기본 비활성화
    [SerializeField] Transform buttonContainer;
    [SerializeField] GameObject pieceButtonPrefab; // TextMeshProUGUI + UnityEngine.UI.Button 필요
    [SerializeField] TextMeshProUGUI promptText;

    readonly List<GameObject> spawnedButtons = new List<GameObject>();
    Action<int> onPicked;

    void Awake()
    {
        if (instance == null) instance = this;
        panelRoot?.SetActive(false);
    }

    public void Show(IReadOnlyList<PieceData> pieces, Action<int> onPicked, string prompt = "카드를 받을 기물을 선택하세요")
    {
        this.onPicked = onPicked;
        ClearButtons();

        if (promptText != null) promptText.text = prompt;

        for (int i = 0; i < pieces.Count; i++)
        {
            int index = i; // 클로저 캡처용 로컬 복사
            GameObject buttonObj = Instantiate(pieceButtonPrefab, buttonContainer);
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = pieces[i].pieceName;

            UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            button?.onClick.AddListener(() => Pick(index));

            spawnedButtons.Add(buttonObj);
        }

        panelRoot?.SetActive(true);
    }

    void Pick(int index)
    {
        panelRoot?.SetActive(false);
        ClearButtons();
        var callback = onPicked;
        onPicked = null;
        callback?.Invoke(index);
    }

    void ClearButtons()
    {
        foreach (var obj in spawnedButtons)
            if (obj != null) Destroy(obj);
        spawnedButtons.Clear();
    }
}
