using System.Collections;
using UnityEngine;

// 캐릭터에 붙는 시각 이펙트 모음. 실드 등 지속 효과는 오빗 회전으로, 회복 등 1회성 효과는 파티클 버스트로 표시한다.
public class PieceEffect : MonoBehaviour
{
    [SerializeField] GameObject orbitPrefab;
    [SerializeField] int count = 1;
    [SerializeField] float radius = 1f;
    [SerializeField] float heightOffset = 0.5f;
    [SerializeField] float degreesPerSecond = 180f;
    [SerializeField] Vector3 axis = Vector3.forward;

    GameObject[] spawned;
    bool visible = true;

    Vector3 Center => transform.position + Vector3.up * heightOffset;

    // 캐릭터(자식 포함) 렌더러 bounds를 합쳐서 가장 위쪽 월드 좌표를 구한다.
    Vector3 Top
    {
        get
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Center;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return new Vector3(transform.position.x, bounds.max.y, transform.position.z);
        }
    }

    void Awake()
    {
        SpawnAll();
    }

    void Update()
    {
        foreach (GameObject instance in spawned)
            instance.transform.RotateAround(Center, axis, degreesPerSecond * Time.deltaTime);
    }

    public void SetCount(int newCount)
    {
        DestroySpawned();
        count = Mathf.Max(0, newCount);
        SpawnAll();
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
        if (spawned == null) return;
        foreach (GameObject instance in spawned)
            instance.SetActive(visible);
    }

    void SpawnAll()
    {
        spawned = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(orbitPrefab, transform);
            Vector3 offset = Quaternion.AngleAxis(360f / count * i, axis) * new Vector3(radius, 0f, 0f);
            instance.transform.position = Center + offset;

            Vector3 outward = (instance.transform.position - Center).normalized;
            instance.transform.rotation = Quaternion.LookRotation(outward, axis);

            instance.SetActive(visible);
            spawned[i] = instance;
        }
    }

    void DestroySpawned()
    {
        if (spawned == null) return;
        foreach (GameObject instance in spawned)
            if (instance != null) Destroy(instance);
    }

    // 회복 시 캐릭터 위치에서 한 번 터지고 사라지는 초록색 파티클 이펙트.
    // 모든 피스가 PieceEffectDatabase에 등록된 같은 프리팹을 공유한다.
    public void PlayHealEffect()
    {
        GameObject prefab = PieceEffectDatabase.instance?.healEffectPrefab;
        if (prefab == null) return;

        StartCoroutine(PlayBurstCoroutine(prefab, Center, Quaternion.identity));
    }

    // 디버프가 적용될 때 캐릭터 제일 위쪽에서, 위에서 아래로 뿌려지는 파티클 이펙트.
    // heal과 같은 종류의 prefab을 180도 뒤집어서 방향만 반대로 재사용한다.
    public void PlayDebuffEffect()
    {
        GameObject prefab = PieceEffectDatabase.instance?.statusEffectPrefab;
        if (prefab == null) return;

        StartCoroutine(PlayBurstCoroutine(prefab, Top, Quaternion.Euler(180f, 0f, 0f)));
    }

    // 코루틴으로 따로 돌기 때문에, 짧은 시간에 여러 번 트리거되어도 각 파티클이 자기 수명을 끝까지 채우고 사라진다.
    IEnumerator PlayBurstCoroutine(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate(prefab, position, rotation, transform);
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;

        yield return new WaitForSeconds(lifetime);

        if (instance != null) Destroy(instance);
    }
}
