using UniVRM10;
using UnityEngine;

// VRM 캐릭터의 표정을 Animator 상태에 맞춰 바꾼다(애니메이션 FBX에는 얼굴 메시가 없어 표정 커브가 없다).
// 상태 이름 = 트리거 이름(Board.PlayTriggerAnimCor 관례). 평상시에는 무작위로 눈을 깜빡인다.
[RequireComponent(typeof(Animator))]
public class VrmFaceExpression : MonoBehaviour
{
    [System.Serializable]
    public struct Face
    {
        [Range(0f, 1f)] public float angry;
        [Range(0f, 1f)] public float blink;
        [Range(0f, 1f)] public float aa;
    }

    [SerializeField] Face attack = new Face { angry = 0.8f, aa = 0.6f };
    [SerializeField] Face hit = new Face { angry = 0.75f, blink = 0.75f, aa = 0.4f };
    [SerializeField] Face death = new Face { blink = 0.9f, aa = 0.5f };
    [SerializeField] Face summon = new Face { angry = 0.85f, aa = 0.55f };
    [SerializeField] float fadeSpeed = 12f;
    [SerializeField] Vector2 blinkInterval = new Vector2(2f, 5f);
    [SerializeField] float blinkDuration = 0.15f;

    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int AreaAttackHash = Animator.StringToHash("AreaAttack");
    static readonly int ShieldHash = Animator.StringToHash("Shield");
    static readonly int BuffHash = Animator.StringToHash("Buff");
    static readonly int HealHash = Animator.StringToHash("Heal");
    static readonly int ApplyStatusHash = Animator.StringToHash("ApplyStatus");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DieHash = Animator.StringToHash("Die");
    static readonly int SummonHash = Animator.StringToHash("Summon");

    Animator animator;
    Vrm10Instance vrm;
    Face current;
    float nextBlinkTime;
    float blinkStartTime = -1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        vrm = GetComponent<Vrm10Instance>();
        ScheduleBlink();
    }

    void Update()
    {
        if (vrm == null) return;
        bool idle;
        Face target = TargetFace(out idle);
        float t = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);
        current.angry = Mathf.Lerp(current.angry, target.angry, t);
        current.blink = Mathf.Lerp(current.blink, target.blink, t);
        current.aa = Mathf.Lerp(current.aa, target.aa, t);

        var expression = vrm.Runtime.Expression;
        expression.SetWeight(ExpressionKey.Angry, current.angry);
        expression.SetWeight(ExpressionKey.Aa, current.aa);
        expression.SetWeight(ExpressionKey.Blink, Mathf.Max(current.blink, idle ? IdleBlink() : 0f));
    }

    Face TargetFace(out bool idle)
    {
        idle = false;
        AnimatorStateInfo state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
        int hash = state.shortNameHash;
        if (hash == HitHash) return hit;
        if (hash == DieHash) return death;
        if (hash == SummonHash) return summon;
        if (hash == AttackHash || hash == AreaAttackHash || hash == ShieldHash || hash == BuffHash || hash == HealHash || hash == ApplyStatusHash) return attack;
        idle = true;
        return default;
    }

    float IdleBlink()
    {
        if (blinkStartTime < 0f && Time.time >= nextBlinkTime) blinkStartTime = Time.time;
        if (blinkStartTime < 0f) return 0f;
        float p = (Time.time - blinkStartTime) / blinkDuration;
        if (p >= 1f)
        {
            blinkStartTime = -1f;
            ScheduleBlink();
            return 0f;
        }
        return 1f - Mathf.Abs(p * 2f - 1f);
    }

    void ScheduleBlink()
    {
        nextBlinkTime = Time.time + Random.Range(blinkInterval.x, blinkInterval.y);
    }
}
