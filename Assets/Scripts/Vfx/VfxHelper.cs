using System.Collections;
using UnityEngine;

// 이펙트 생성·정리와 보드 좌표 → 월드 좌표 변환 공용 헬퍼. 기물 상시 이펙트(PieceEffect)와 공격 이펙트(AttackVfxCue)가 함께 쓴다.
public static class VfxHelper
{
    // prefab을 생성하고, 파티클이면 tint로 시작 색을 바꾼다.
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Color? tint = null)
    {
        if (prefab == null) return null;
        GameObject instance = parent != null
            ? Object.Instantiate(prefab, position, rotation, parent)
            : Object.Instantiate(prefab, position, rotation);
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (tint.HasValue && ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = tint.Value;
        }
        return instance;
    }

    // 파티클이면 재생 시간 + 입자 최대 수명, 아니면 fallback만큼 기다렸다가 파괴한다.
    // 코루틴으로 따로 돌기 때문에, 짧은 시간에 여러 번 생성되어도 각 이펙트가 자기 수명을 끝까지 채우고 사라진다.
    public static IEnumerator DestroyAfterLifetime(GameObject instance, float fallback = 2f)
    {
        if (instance == null) yield break;
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : fallback;
        yield return new WaitForSeconds(lifetime);
        if (instance != null) Object.Destroy(instance);
    }

    static Material defaultLineMaterial;

    // 프리팹 없이 그리는 선(레이저·원형 이펙트용). material이 없으면 정점 색을 그대로 쓰는 기본 머티리얼.
    public static LineRenderer CreateLine(string name, float width, Color color, Material material, int positionCount, bool loop)
    {
        var go = new GameObject(name);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = positionCount;
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = material != null ? material : DefaultLineMaterial();
        return line;
    }

    static Material DefaultLineMaterial()
    {
        if (defaultLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            defaultLineMaterial = new Material(shader);
        }
        return defaultLineMaterial;
    }

    // 보드 칸의 월드 좌표(기물이 서는 자리).
    public static Vector3 CellPosition(Vector2Int cell)
    {
        return Board.instance != null ? Board.instance.CellWorldPosition(cell) : Vector3.zero;
    }

    // 기물 렌더러 bounds를 합친 중심 — 피격점·레이저 끝점으로 쓴다. 렌더러가 없으면 기물 위치.
    public static Vector3 PieceCenter(Piece piece)
    {
        if (piece == null) return Vector3.zero;
        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return piece.transform.position;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.center;
    }

    // 기물 자식 중 id가 같은 VfxSocket(손·무기 끝 등). 없으면 null — 호출부가 PieceCenter로 대신한다.
    public static Transform FindSocket(Piece piece, string id)
    {
        if (piece == null || string.IsNullOrEmpty(id)) return null;
        foreach (VfxSocket socket in piece.GetComponentsInChildren<VfxSocket>(true))
            if (socket.id == id) return socket.transform;
        return null;
    }

    // 소켓이 있으면 소켓 위치, 없으면 기물 중심. 소켓은 애니메이션으로 움직이므로 매 프레임 다시 읽어야 한다.
    public static Vector3 SocketOrCenter(Piece piece, Transform socket)
    {
        return socket != null ? socket.position : PieceCenter(piece);
    }
}
