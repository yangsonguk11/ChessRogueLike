using System.Collections.Generic;
using UnityEngine;

// CardDatabase가 실제로 제공하는 기능만 담은 좁은 인터페이스. 소비하는 쪽(ShopCanvas 등)이
// CardDatabase라는 구체 클래스가 아니라 이 인터페이스에 의존하게 하기 위한 DIP 경계.
public interface ICardDatabase
{
    GameObject SpawnCard(RectTransform handParent, string cardName);
    GameObject SpawnSprite(RectTransform handParent, string cardName);

    // 카드 풀 전체에서 중복 없이 무작위로 count개의 카드 이름을 뽑는다. exclude에 담긴 이름은 후보에서 제외.
    // count가 (제외 후) 남은 풀 크기보다 크면 있는 만큼만 반환한다.
    List<string> PickRandomDistinct(int count, IEnumerable<string> exclude = null);

    // 등록된 카드 전체의 이름. 카드 테스트 도구처럼 "전체 목록"이 필요한 곳에서 cardPrefabs를 직접
    // 순회하는 대신 이걸 쓰면 된다.
    IEnumerable<string> GetAllCardNames();
}
