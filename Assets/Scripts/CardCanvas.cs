using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class CardCanvas : MonoBehaviour
{
    public static CardCanvas instance;

    [SerializeField] CardDatabase cardData;
    [SerializeField] Board board;
    public List<RectTransform> cards = new List<RectTransform>(); // �տ� �� ī���
    public List<RectTransform> Discardcards = new List<RectTransform>();
    public Queue<RectTransform> Deckcards = new Queue<RectTransform>();
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius; // ���� ������ (Ŭ���� �ϸ���)
    [SerializeField] float angleBetween;  // ī�� ������ ����
    [SerializeField] float heightOffset; // ��ä���� ���� ����

    int _currentenergy;
    public int currentenergy { get { return _currentenergy; } set { _currentenergy = value; UpdateCurrentEnergy(); } }
    public int maxenergy = 3;
    RectTransform nowusingCard;
    public bool isCardEffecting;
    private void Awake()
    {
        if (instance == null) instance = this;
        currentenergy = 3;
        foreach(string cardName in DataManager.Instance.currentData.deckCardIDs)
        {
            GameObject obj = cardData.SpawnCard(GetComponent<RectTransform>(), cardName);
            if(obj != null)
            {
                Discardcards.Add(obj.GetComponent<RectTransform>());
                obj.GetComponent<RectTransform>().position = DiscardZone.position;
            }
        }
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    public void CardSelected(int handNum)   //���� ī�带 ������ ��
    {
        if (handNum == -1)
            return;
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
        HandZone.GetComponent<Image>().raycastTarget = true;
    }

    public void CardUnSelected()            //���� ī�带 ���� ��
    {
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }
    public void UseCard(int handnum)        //���� ī�带 ��� ���� ����� ��
    {
        Debug.Log(cards[handnum].GetComponent<Card>().Cost > currentenergy);
        Debug.Log(isCardEffecting);
        Debug.Log(TurnManager.instance.currentState != TurnState.Player);
        if (cards[handnum].GetComponent<Card>().Cost > currentenergy || isCardEffecting || TurnManager.instance.currentState != TurnState.Player)
            return;
        ClearnowusingCard();
        nowusingCard = cards[handnum];
        nowusingCard.GetComponent<Card>().handNumber = -1;
        cards.RemoveAt(handnum);
        nowusingCard.position = CardNowUsingPos.position;
        nowusingCard.localRotation = Quaternion.Euler(0, 0, 0);
        AlignCards();
        board.UseCard(nowusingCard.GetComponent<Card>());
    }

    public void ClearnowusingCard()         //������� ī�� �ʱ�ȭ
    {
        if (nowusingCard)
        {
            cards.Add(nowusingCard);
        }
        nowusingCard = null;
        AlignCards();
    }

    public void HandtoDiscardAll()
    {
        Debug.LogFormat("{0} ��", cards.Count);
        while(cards.Count > 0)
        {
            HandtoDiscard(0);
        }
    }

    public void HandtoDiscard(int num)
    {
        Debug.LogFormat("{0} {1}", cards.Count, num);
        if (cards.Count <= num)
            return;
        HandtoDiscard(cards[num]);
    }

    public void HandtoDiscard(RectTransform card)
    {
        Discardcards.Add(card);
        card.position = DiscardZone.position;
        cards.Remove(card);
    }
    public void DrawTurnStartCards()
    {
        DrawCard();
        DrawCard();
        DrawCard();
        DrawCard();
        DrawCard();
    }
    public void FinishUseCard()             //���� ī�� ��� ����
    {
        if (nowusingCard)
        {
            HandtoDiscard(nowusingCard);
            currentenergy -= nowusingCard.GetComponent<Card>().Cost;
        }

        nowusingCard = null;
        isCardEffecting = false;
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/{1}", currentenergy, maxenergy);
    }

    void DrawCard()                         //���� ī�带 ������ ��������
    {

        if (Deckcards.Count == 0)
        {
            DiscardtoDeck();
            Debug.Log("nodeck");
        }
        if (Deckcards.Count != 0)
        {
            cards.Add(Deckcards.Dequeue());
        }
        AlignCards();
    }

    void DiscardtoDeck()
    {
        if (Discardcards.Count == 0) return;

        var random = new System.Random();

        // 1. LINQ�� ����Ͽ� ����Ʈ�� �������� ���� �ٽ� ����Ʈ�� ��ȯ
        var shuffledCards = Discardcards.OrderBy(x => random.Next()).ToList();

        // 2. ���� ī����� ��(Queue)�� ������� �ֱ�
        foreach (var card in shuffledCards)
        {
            Deckcards.Enqueue(card);
        }

        // 3. ���� ���� ī�� ����Ʈ ����
        Discardcards.Clear();
    }

    public void GetMaxEnergy()
    {
        currentenergy = maxenergy;
    }
    [ContextMenu("Align Cards")] // �ν����� �޴����� �ٷ� ���� ����
    public void AlignCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        // ��ü ���� ���� ��� (����� 0���� ����)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleBetween);

            // 1. ��ġ ��� (�ﰢ�Լ� ���)
            // �������� ��ȯ: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // ���� ���κп� ����

            // 2. ī�� ��ǥ �� ȸ�� ����
            cards[i].SetSiblingIndex(i);
            cards[i].localPosition = new Vector3(x, y + heightOffset, 0);
            cards[i].localRotation = Quaternion.Euler(0, 0, -currentAngle);

            cards[i].gameObject.GetComponent<Card>().OnUnSelected -= CardUnSelected;
            cards[i].gameObject.GetComponent<Card>().OnUnSelected += CardUnSelected;
            cards[i].gameObject.GetComponent<Card>().cardCanvas = gameObject;
            cards[i].gameObject.GetComponent<Card>().handNumber = i;
        }
    }
    public void ExcludeAlignCards(int excludeCard = -1)
    {
        int count;
        if (excludeCard < 0)
            count = cards.Count;
        else
            count = cards.Count - 1;
        if (count == 0) return;

        // ��ü ���� ���� ��� (����� 0���� ����)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleBetween);

            // 1. ��ġ ��� (�ﰢ�Լ� ���)
            // �������� ��ȯ: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // ���� ���κп� ����

            // 2. ī�� ��ǥ �� ȸ�� ����
            if (i >= excludeCard && excludeCard >= 0)
            {
                cards[i+1].SetSiblingIndex(i);
                cards[i+1].localPosition = new Vector3(x, y + heightOffset, 0);
                cards[i+1].localRotation = Quaternion.Euler(0, 0, -currentAngle);
            }
            else
            {
                cards[i].SetSiblingIndex(i);
                cards[i].localPosition = new Vector3(x, y + heightOffset, 0);
                cards[i].localRotation = Quaternion.Euler(0, 0, -currentAngle);
            }
        }
        if(excludeCard >= 0)
            cards[excludeCard].SetAsLastSibling();

    }
}