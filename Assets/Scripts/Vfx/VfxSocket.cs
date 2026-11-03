using UnityEngine;

// 공격 이펙트가 나가는 기물의 부위(손, 지팡이 끝 등) 표시. 기물 프리팹의 본(자식)에 붙이고 id를 적으면,
// AttackVfxCue.socketId가 같은 이펙트가 그 위치에서 나간다. 소켓이 없는 기물은 기물 중심에서 나간다.
public class VfxSocket : MonoBehaviour
{
    public string id;
}
