using UnityEngine;
using System.Collections.Generic;
using System;

public class CardCanvas : MonoBehaviour
{
    public List<RectTransform> cards = new List<RectTransform>(); // 손에 든 카드들
    public float radius = 1000f;      // 원의 반지름 (클수록 완만함)
    public float angleBetween = 10f;  // 카드 사이의 각도
    public float heightOffset = 100f; // 부채꼴의 높이 보정

    private void Awake()
    {
        int i = 0;
        foreach(RectTransform card in cards)
        {
            
            Debug.Log("asdf");
            //card.gameObject.GetComponent<Card>().OnSelected += AlignCards;
            card.gameObject.GetComponent<Card>().OnUnSelected += AlignCards;
            card.gameObject.GetComponent<Card>().handNumber = i++;
            card.gameObject.GetComponent<Card>().cardCanvas = gameObject;
        }
    }

    public void CardSelected(int handNum)
    {
        cards[handNum].GetComponent<Card>().SelectedTrue();
        ExcludeAlignCards(cards[handNum].GetComponent<Card>().handNumber);
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