using System.Collections.Generic;
using UnityEngine;

// 기물 1명이 소유한 카드 자원(손패/덱/버림더미/소멸더미). 에너지는 아군 전체가 공유하므로 여기 없음
// (CardCanvas가 따로 들고 있음). MonoBehaviour가 아닌 순수 데이터 홀더로, Piece가 들고 있고
// CardCanvas가 activePiece를 통해 이 상태를 화면에 보여준다.
public class PieceDeck
{
    public List<RectTransform> Hand = new List<RectTransform>();
    public List<RectTransform> Discard = new List<RectTransform>();
    public Queue<RectTransform> Deck = new Queue<RectTransform>();
    public List<RectTransform> Exile = new List<RectTransform>();
}
