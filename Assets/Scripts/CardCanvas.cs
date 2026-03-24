using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

public class CardCanvas : MonoBehaviour
{
    [SerializeField] Board board;
    public List<RectTransform> cards = new List<RectTransform>(); // 손에 든 카드들
    public List<RectTransform> Discardcards = new List<RectTransform>();
    public Queue<RectTransform> Deckcards = new Queue<RectTransform>();
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] RectTransform DiscardZone;
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius; // 원의 반지름 (클수록 완만함)
    [SerializeField] float angleBetween;  // 카드 사이의 각도
    [SerializeField] float heightOffset; // 부채꼴의 높이 보정

    int _currentenergy;
    public int currentenergy { get { return _currentenergy; } set { _currentenergy = value; UpdateCurrentEnergy(); } }
    public int maxenergy = 99;
    RectTransform nowusingCard;
    public bool isCardEffecting;
    private void Awake()
    {
        currentenergy = 99;
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    public void CardSelected(int handNum)   //손의 카드를 눌렀을 때
    {
        if (handNum == -1)
            return;
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
        HandZone.GetComponent<Image>().raycastTarget = true;
    }

    public void CardUnSelected()            //손의 카드를 뗐을 때
    {
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }
    public void UseCard(int handnum)        //손의 카드를 끌어서 놓아 사용할 때
    {
        if (cards[handnum].GetComponent<Card>().Cost > currentenergy || isCardEffecting || TurnManager.instance.currentState != TurnState.Player)
            return;
        ClearnowusingCard();
        nowusingCard = cards[handnum];
        nowusingCard.GetComponent<Card>().handNumber = -1;
        board.UseCard(nowusingCard.GetComponent<Card>());

        nowusingCard.position = CardNowUsingPos.position;
        nowusingCard.localRotation = Quaternion.Euler(0, 0, 0);
        cards.RemoveAt(handnum);
        AlignCards();
    }

    public void ClearnowusingCard()         //사용중인 카드 초기화
    {
        if (nowusingCard)
        {
            cards.Add(nowusingCard);
        }
        nowusingCard = null;
        AlignCards();
    }
    public void FinishUseCard()             //손의 카드 사용 이후
    {
        Discardcards.Add(nowusingCard);
        currentenergy -= nowusingCard.GetComponent<Card>().Cost;
        nowusingCard.position = DiscardZone.position;
        nowusingCard = null;
        isCardEffecting = false;
        if (cards.Count <= 0)
        {
            DrawCard();
            DrawCard();
            DrawCard();
            DrawCard();
        }
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/{1}", currentenergy, maxenergy);
    }

    void DrawCard()                         //덱의 카드를 손으로 가져오기
    {
        if (Deckcards.Count == 0)
        {
            DiscardtoDeck();                
            
        }
        if (Deckcards.Count != 0)
        {
            cards.Add(Deckcards.Dequeue());
        }
        AlignCards();
    }

    void DiscardtoDeck()
    {
        while (Discardcards.Count != 0)
        {
            Deckcards.Enqueue(Discardcards[0]);
            Discardcards.RemoveAt(0);
        }
    }

    //[ContextMenu("Align Cards")] // 인스펙터 메뉴에서 바로 실행 가능
    public void AlignCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        // 전체 각도 범위 계산 (가운데를 0도로 잡음)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleBetween);

            // 1. 위치 계산 (삼각함수 사용)
            // 라디안으로 변환: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // 원의 윗부분에 맞춤

            // 2. 카드 좌표 및 회전 적용
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

        // 전체 각도 범위 계산 (가운데를 0도로 잡음)
        float totalAngle = (count - 1) * angleBetween;
        float startAngle = -totalAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleBetween);

            // 1. 위치 계산 (삼각함수 사용)
            // 라디안으로 변환: Degree * (PI / 180)
            float radian = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radian) * radius;
            float y = Mathf.Cos(radian) * radius - radius; // 원의 윗부분에 맞춤

            // 2. 카드 좌표 및 회전 적용
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