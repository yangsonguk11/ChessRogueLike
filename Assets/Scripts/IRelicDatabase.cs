using System.Collections.Generic;
using UnityEngine;

public interface IRelicDatabase
{
    Relic CreateRelic(string relicName);
    GameObject SpawnIcon(Transform parent, string relicName);

    // 유물 아이콘 풀 전체에서 중복 없이 무작위로 count개의 유물 이름을 뽑는다. exclude에 담긴 이름은 제외.
    List<string> PickRandomDistinct(int count, IEnumerable<string> exclude = null);
}
