using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 화면 하단에서 화자/대사를 한 글자씩 보여주고, 확인 버튼과 선택지 버튼을 동적으로 생성하는 대화창
public class DialogueUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI speakerText;
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] GameObject choiceButtonPrefab;  // 확인/선택지 버튼 공용 프리팹 (TextMeshProUGUI 포함)
    [SerializeField] Transform choiceContainer;      // 확인/선택지 버튼이 생성될 부모
    [SerializeField] string confirmButtonText = "확인";
    [SerializeField] float charInterval = 0.03f;     // 한 글자가 나오는 간격(초)

    List<DialogueSO.Line> lines;
    int currentIndex;
    readonly List<GameObject> spawnedButtons = new List<GameObject>();
    Coroutine typingRoutine;

    public void Show(DialogueSO dialogue)
    {
        if (dialogue == null) return;
        Show(dialogue.lines);
    }

    public bool IsShowing => gameObject.activeSelf;

    public void Show(List<DialogueSO.Line> dialogueLines)
    {
        if (IsShowing) return; // 이미 대화 중이면 새 대화로 덮어쓰지 않음

        lines = dialogueLines;
        currentIndex = 0;
        gameObject.SetActive(true);
        DisplayCurrentLine();
    }

    void DisplayCurrentLine()
    {
        ClearButtons();
        StopTyping();

        if (lines == null || currentIndex < 0 || currentIndex >= lines.Count)
        {
            FinishDialogue();
            return;
        }

        DialogueSO.Line line = lines[currentIndex];
        if (speakerText != null) speakerText.text = line.speaker;

        typingRoutine = StartCoroutine(TypeText(line));
    }

    IEnumerator TypeText(DialogueSO.Line line)
    {
        if (dialogueText != null) dialogueText.text = "";

        string text = line.text ?? "";
        for (int i = 0; i < text.Length; i++)
        {
            if (dialogueText != null) dialogueText.text += text[i];
            yield return new WaitForSeconds(charInterval);
        }

        typingRoutine = null;

        bool isChoice = line.type == DialogueSO.LineType.Choice && line.choices != null && line.choices.Count > 0;
        if (isChoice)
            SpawnChoiceButtons(line.choices);
        else
            SpawnConfirmButton();
    }

    void StopTyping()
    {
        if (typingRoutine == null) return;
        StopCoroutine(typingRoutine);
        typingRoutine = null;
    }

    void SpawnConfirmButton()
    {
        SpawnButton(confirmButtonText, OnClickConfirm);
    }

    void SpawnChoiceButtons(List<DialogueSO.Choice> choices)
    {
        foreach (var choice in choices)
        {
            DialogueSO.Choice c = choice;
            SpawnButton(c.choiceText, () => OnChoiceSelected(c));
        }
    }

    void SpawnButton(string text, System.Action onClick)
    {
        if (choiceButtonPrefab == null || choiceContainer == null) return;

        GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);

        TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        label?.SetText(text);

        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        button?.onClick.AddListener(() => onClick());

        spawnedButtons.Add(buttonObj);
    }

    void OnChoiceSelected(DialogueSO.Choice choice)
    {
        if (choice.damageAmount > 0)
            Board.instance?.DamageAllAllies(choice.damageAmount);

        if (choice.healAmount != 0)
            Board.instance?.HealAllAllies(amount: choice.healAmount);

        if (choice.cardPool != null && choice.cardPool.Count > 0)
        {
            string cardName = choice.cardPool[Random.Range(0, choice.cardPool.Count)];
            DataManager.Instance.AddCardOnDeck(cardName);
        }

        if (choice.triggerCombat != null)
        {
            Hide();
            Board.instance?.EnterCombat(choice.triggerCombat);
            return;
        }

        if (choice.nextLineIndex < 0)
        {
            FinishDialogue();
            return;
        }
        currentIndex = choice.nextLineIndex;
        DisplayCurrentLine();
    }

    void ClearButtons()
    {
        foreach (var obj in spawnedButtons)
            Destroy(obj);
        spawnedButtons.Clear();
    }

    // 타이핑이 끝난 뒤 생성된 확인 버튼의 OnClick에 코드로 연결됨
    void OnClickConfirm()
    {
        currentIndex++;
        DisplayCurrentLine();
    }

    public void Hide()
    {
        StopTyping();
        ClearButtons();
        gameObject.SetActive(false);
    }

    void FinishDialogue()
    {
        Hide();
        Board.instance?.OnDialogueClosed();
    }
}
