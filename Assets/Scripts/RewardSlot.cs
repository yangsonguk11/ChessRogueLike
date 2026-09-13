using System;
using TMPro;
using UnityEngine;

// 보상 그리드에 스폰되는 범용 버튼. ShopCardSlot과 동일한 "슬롯 래퍼" 패턴이지만
// 실제 카드 비주얼 대신 텍스트 라벨만 표시한다(카드 비주얼은 3장 선택 패널에서만 보여줌).
// 보상을 받으면 비활성화하는 대신 ResultCanvas가 슬롯 자체를 Destroy하므로,
// 이 컴포넌트는 라벨 설정과 클릭 콜백 연결만 담당한다.
// 프로젝트에 전역 class Button(보드 타일 버튼, Assets/Scripts/Button.cs)이 이미 있어
// UnityEngine.UI.Button을 가리므로, 다른 코드(ShopCanvas 등)와 마찬가지로 완전 정규화해서 쓴다.
public class RewardSlot : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Button button;
    [SerializeField] TextMeshProUGUI labelText;

    public void SetLabel(string text)
    {
        if (labelText != null) labelText.text = text;
    }

    public void SetOnClick(Action onClick)
    {
        button.onClick.RemoveAllListeners();
        if (onClick != null) button.onClick.AddListener(() => onClick());
    }
}
