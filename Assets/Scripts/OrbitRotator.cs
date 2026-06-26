using UnityEngine;

// 캐릭터에 붙이면 자식으로 회전 오브젝트를 자동 생성하고, 캐릭터를 중심으로 회전시킨다.
public class OrbitRotator : MonoBehaviour
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
}
