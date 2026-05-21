using UnityEngine;
using System.Collections;
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
    [SerializeField] RectTransform DeckZone;
    [SerializeField] RectTransform ExileZone;
    public List<RectTransform> Exilecards = new List<RectTransform>();
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius; // ���� ������ (Ŭ���� �ϸ���)
    [SerializeField] float angleBetween;  // ī�� ������ ����
    [SerializeField] float heightOffset; // ��ä���� ���� ����

    int _currentenergy;
    public int currentenergy { get { return _currentenergy; } set { _currentenergy = value; UpdateCurrentEnergy(); } }
    public int maxenergy = 3;
    RectTransform nowusingCard;
    public bool isCardEffecting;
    Coroutine pendingCardCoroutine;
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
                obj.GetComponent<RectTransform>().position = DeckZone.position;
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
        Card card = cards[handnum].GetComponent<Card>();
        // isCardEffecting이면서 nowusingCard가 없으면 보드가 실제 효과 처리 중 → 차단
        // nowusingCard가 있으면 아직 애니메이션/대기 중이므로 교체 허용
        if (card.Cost > currentenergy || (isCardEffecting && nowusingCard == null) || TurnManager.instance.currentState != TurnState.Player || !card.CanUse())
            return;
        if (pendingCardCoroutine != null)
        {
            StopCoroutine(pendingCardCoroutine);
            pendingCardCoroutine = null;
        }
        ClearnowusingCard();
        nowusingCard = cards[handnum];
        nowusingCard.GetComponent<Card>().handNumber = -1;
        cards.RemoveAt(handnum);
        AlignCards();
        pendingCardCoroutine = StartCoroutine(AnimateCardToUsingPos(nowusingCard));
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
        StartCoroutine(DrawCardsWithDelay(5, 0.15f));
    }

    IEnumerator DrawCardsWithDelay(int count, float delay)
    {
        var newCards = new List<RectTransform>();
        for (int i = 0; i < count; i++)
        {
            if (Deckcards.Count == 0) DiscardtoDeck();
            if (Deckcards.Count == 0) break;
            var card = Deckcards.Dequeue();
            cards.Add(card);
            newCards.Add(card);
        }
        if (newCards.Count == 0) yield break;

        AlignCards();

        var targetPositions = new List<Vector3>();
        var targetRotations = new List<Quaternion>();
        foreach (var c in newCards)
        {
            targetPositions.Add(c.position);
            targetRotations.Add(c.localRotation);
            c.position = DeckZone.position;
            c.localRotation = Quaternion.identity;
        }

        for (int i = 0; i < newCards.Count; i++)
        {
            StartCoroutine(MoveCard(newCards[i], targetPositions[i], targetRotations[i], 0.3f));
            if (i < newCards.Count - 1)
                yield return new WaitForSeconds(delay);
        }
    }
    public void RefreshAllCardViews()
    {
        foreach (var rt in cards)
            rt.GetComponent<Card>()?.RefreshView();
    }

    public void FinishUseCard()             //���� ī�� ��� ����
    {
        RefreshAllCardViews();
        if (nowusingCard)
        {
            Card card = nowusingCard.GetComponent<Card>();
            currentenergy -= card.Cost;
            RectTransform usedCard = nowusingCard;
            nowusingCard = null;
            if (card.exileOnUse)
            {
                Exilecards.Add(usedCard);
                StartCoroutine(AnimateCardToExileAndFinish(usedCard));
            }
            else
            {
                StartCoroutine(AnimateCardToDiscard(usedCard));
            }
        }
        else
        {
            isCardEffecting = false;
        }
    }

    IEnumerator AnimateCardToExileAndFinish(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, ExileZone.position, Quaternion.identity, 0.25f));
        isCardEffecting = false;
    }

    IEnumerator AnimateCardToUsingPos(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, CardNowUsingPos.position, Quaternion.identity, 0.35f));
        pendingCardCoroutine = null;
        board.UseCard(card.GetComponent<Card>());
    }

    IEnumerator AnimateCardToDiscard(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, DiscardZone.position, Quaternion.identity, 0.15f));
        Discardcards.Add(card);
        isCardEffecting = false;
    }

    IEnumerator MoveCard(RectTransform card, Vector3 targetWorldPos, Quaternion targetLocalRot, float duration)
    {
        Vector3 startPos = card.position;
        Quaternion startRot = card.localRotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            card.position = Vector3.Lerp(startPos, targetWorldPos, smooth);
            card.localRotation = Quaternion.Lerp(startRot, targetLocalRot, smooth);
            yield return null;
        }
        card.position = targetWorldPos;
        card.localRotation = targetLocalRot;
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/{1}", currentenergy, maxenergy);
    }

    public void DrawCard()                         //���� ī�带 ������ ��������
    {

        if (Deckcards.Count == 0)
        {
            DiscardtoDeck();
            Debug.Log("nodeck");
        }
        if (Deckcards.Count != 0)
        {
            RectTransform newCard = Deckcards.Dequeue();
            cards.Add(newCard);
            AlignCards();
            Vector3 targetPos = newCard.position;
            Quaternion targetRot = newCard.localRotation;
            newCard.position = DeckZone.position;
            newCard.localRotation = Quaternion.identity;
            StartCoroutine(MoveCard(newCard, targetPos, targetRot, 0.3f));
        }
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
            card.position = DeckZone.position;
            Deckcards.Enqueue(card);
        }

        // 3. ���� ���� ī�� ����Ʈ ����
        Discardcards.Clear();
    }

    public void GetMaxEnergy()
    {
        currentenergy = maxenergy;
    }

    public void HandtoDiscardCount(int count)
    {
        int n = (count <= 0) ? cards.Count : Mathf.Min(count, cards.Count);
        for (int i = 0; i < n; i++)
        {
            if (cards.Count == 0) break;
            int idx = UnityEngine.Random.Range(0, cards.Count);
            HandtoDiscard(cards[idx]);
        }
        AlignCards();
    }

    public void HandtoDeckCount(int count)
    {
        int n = (count <= 0) ? cards.Count : Mathf.Min(count, cards.Count);
        var handSnapshot = new List<RectTransform>(cards);
        for (int i = 0; i < n && handSnapshot.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, handSnapshot.Count);
            RectTransform card = handSnapshot[idx];
            handSnapshot.RemoveAt(idx);
            cards.Remove(card);
            card.position = DeckZone.position;
            Deckcards.Enqueue(card);
        }
        var list = Deckcards.ToList().OrderBy(x => UnityEngine.Random.value).ToList();
        Deckcards = new Queue<RectTransform>(list);
        AlignCards();
    }

    public void HandtoExileCount(int count)
    {
        int n = (count <= 0) ? cards.Count : Mathf.Min(count, cards.Count);
        var handSnapshot = new List<RectTransform>(cards);
        var toExile = new List<RectTransform>();
        for (int i = 0; i < n && handSnapshot.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, handSnapshot.Count);
            toExile.Add(handSnapshot[idx]);
            handSnapshot.RemoveAt(idx);
        }
        foreach (var card in toExile)
            ExileCard(card);
    }

    public void HandtoDeckTop(int count)
    {
        int n = (count <= 0) ? cards.Count : Mathf.Min(count, cards.Count);
        var handSnapshot = new List<RectTransform>(cards);
        var toReturn = new List<RectTransform>();
        for (int i = 0; i < n && handSnapshot.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, handSnapshot.Count);
            toReturn.Add(handSnapshot[idx]);
            handSnapshot.RemoveAt(idx);
        }
        foreach (var card in toReturn)
        {
            cards.Remove(card);
            card.position = DeckZone.position;
        }
        var newDeck = toReturn.Concat(Deckcards.ToList()).ToList();
        Deckcards = new Queue<RectTransform>(newDeck);
        AlignCards();
    }

    public void ExileHandCard(int handnum)
    {
        if (handnum < 0 || handnum >= cards.Count) return;
        ExileCard(cards[handnum]);
    }

    public void ExileCard(RectTransform card)
    {
        bool wasInHand = cards.Remove(card);
        if (!wasInHand)
        {
            if (!Discardcards.Remove(card))
            {
                var deckList = Deckcards.ToList();
                if (deckList.Remove(card))
                    Deckcards = new Queue<RectTransform>(deckList);
            }
        }
        if (nowusingCard == card)
            nowusingCard = null;

        Exilecards.Add(card);
        StartCoroutine(AnimateCardToExile(card));

        if (wasInHand)
            AlignCards();
    }

    IEnumerator AnimateCardToExile(RectTransform card)
    {
        yield return StartCoroutine(MoveCard(card, ExileZone.position, Quaternion.identity, 0.25f));
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