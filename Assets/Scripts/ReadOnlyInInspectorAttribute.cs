using UnityEngine;

// Awake()에서 코드가 항상 덮어쓰거나 아예 읽지 않는 필드에 붙인다.
// 인스펙터에는 계속 보이지만(값 확인용) 편집은 막아서, 채워도 무시되는 필드에 시간 쓰는 걸 막는다.
public class ReadOnlyInInspectorAttribute : PropertyAttribute
{
}
