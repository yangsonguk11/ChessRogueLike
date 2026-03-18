using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

public class CardCanvas : MonoBehaviour
{
    [SerializeField] Board board;
    public List<RectTransform> cards = new List<RectTransform>(); // 손에 든 카드들
    List<RectTransform> Discardcards = new List<RectTransform>();
    [SerializeField] GameObject HandZone;
    [SerializeField] RectTransform CardNowUsingPos;
    [SerializeField] TextMeshProUGUI CurrentEnergyText;
    [SerializeField] float radius; // 원의 반지름 (클수록 완만함)
    [SerializeField] float angleBetween;  // 카드 사이의 각도
    [SerializeField] float heightOffset; // 부채꼴의 높이 보정

    int _currentenergy;
    public int currentenergy { get { return _currentenergy; } set { _currentenergy = value; UpdateCurrentEnergy(); } }

    RectTransform nowusingCard;
    private void Awake()
    {
        currentenergy = 3;
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }

    public void CardSelected(int handNum)
    {
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
        HandZone.GetComponent<Image>().raycastTarget = true;
    }

    public void CardUnSelected()
    {
        AlignCards();
        HandZone.GetComponent<Image>().raycastTarget = false;
    }
    public void UseCard(int handnum)
    {
        if (cards[handnum].GetComponent<Card>().Cost > currentenergy)
            return;
        if (nowusingCard)
        {
            cards.Add(nowusingCard);
        }
        nowusingCard = cards[handnum];
        board.UseCard(nowusingCard.GetComponent<Card>());

        nowusingCard.position = CardNowUsingPos.position;
        nowusingCard.localRotation = Quaternion.Euler(0, 0, 0);
        cards.RemoveAt(handnum);
        AlignCards();
    }

    public void FinishUseCard()
    {
        Discardcards.Add(nowusingCard);
        currentenergy -= nowusingCard.GetComponent<Card>().Cost;
        Destroy(nowusingCard.gameObject);
    }
    private void UpdateCurrentEnergy()
    {
        CurrentEnergyText.text = string.Format("{0}/3", currentenergy);
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