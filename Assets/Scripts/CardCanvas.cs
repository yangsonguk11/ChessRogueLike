using UnityEngine;
using System.Collections.Generic;

public class CardCanvas : MonoBehaviour
{
    public List<RectTransform> cards = new List<RectTransform>(); // 손에 든 카드들
    public float radius = 1000f;      // 원의 반지름 (클수록 완만함)
    public float angleBetween = 10f;  // 카드 사이의 각도
    public float heightOffset = 100f; // 부채꼴의 높이 보정

    [ContextMenu("Align Cards")] // 인스펙터 메뉴에서 바로 실행 가능
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
            cards[i].localPosition = new Vector3(x, y + heightOffset, 0);
            cards[i].localRotation = Quaternion.Euler(0, 0, -currentAngle);
        }
    }
}