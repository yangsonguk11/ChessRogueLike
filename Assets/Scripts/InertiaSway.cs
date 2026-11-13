// InertiaSway.cs
// 애니메이션이 만든 몸통(Hips)의 가속·감속·비틀림을 감쇠 스프링에 넣어
// 척추~머리에 "관성으로 쏠렸다가 출렁이며 돌아오는" 회전을 애니메이션 위에 덧입히는 컴포넌트.
//  - 앞뒤·좌우 가속도: Hips의 루트 기준 위치를 미분 → 상체가 반대로 쏠렸다가 출렁이며 복귀
//  - 비틀림(Yaw) 각가속도: 하체가 먼저 돌면 상체는 늦게 따라오며 출렁임
// 루트 Transform의 이동(DOMove 등)은 입력으로 쓰지 않는다 — 애니메이션 동작에만 반응한다.
// 손으로 키를 찍은 애니메이션을 대체하는 게 아니라, 그 위에 '매번 조금씩 다른' 잔진동을 더하는 용도.
//
// 주의
//  - Animator가 매 프레임 덮어쓰는 본에만 사용할 것(Humanoid 근육 본은 OK).
//    Generic 리그에서 커브가 없는 본에 쓰면 회전이 누적됨.
//  - Hips는 회전시키는 척추 체인의 부모라 우리가 준 회전이 입력으로 되먹임되지 않는다.
//  - 헤어/옷 스프링본(Magica Cloth, SpringBone 등)보다 '먼저' 실행되어야
//    머리카락이 이 흔들림까지 따라감 → 실행 순서를 확인할 것.
//    (Vrm10Instance는 11000이라 이 컴포넌트(100)가 먼저 돈다.)
//  - 히트스톱(timeScale=0) 중에는 멈추고, 풀리면 이어서 진동.
//  - chain이 비어 있으면 같은 오브젝트 Animator의 Humanoid 본(Spine/Chest/Neck/Head)을 자동으로 쓴다.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class InertiaSway : MonoBehaviour
{
    [System.Serializable]
    public struct Joint
    {
        public Transform bone;
        [Range(0f, 1f)] public float weight; // 체인 전체 합이 1 근처가 되도록 (예: Spine .3 / Chest .3 / Neck .2 / Head .2)
    }

    [Tooltip("Hips 움직임을 재는 기준 좌표계. 비우면 이 오브젝트")]
    [SerializeField] Transform root;
    [Tooltip("비워 두면 Humanoid Animator에서 Spine/Chest/Neck/Head를 자동 지정")]
    [SerializeField] Joint[] chain;

    [Header("Spring")]
    [Tooltip("출렁임 주기(Hz). 2~4 사이가 사람 상체 느낌")]
    [SerializeField] float frequency = 3f;
    [Tooltip("감쇠비 0~1. 낮을수록 오래 출렁임 (0.3~0.5 권장)")]
    [Range(0.05f, 1f)] [SerializeField] float damping = 0.35f;

    [Header("Input (Hips 가속도)")]
    [Tooltip("Hips의 루트 기준 가속도 입력 배율. 0이면 끔")]
    [SerializeField] float animGain = 3f;
    [Tooltip("가속도 1 m/s² 당 목표 기울기(도)")]
    [SerializeField] float degreesPerAccel = 2.5f;
    [SerializeField] float maxAngle = 20f;
    [Tooltip("Hips 가속도(m/s², 앞뒤·좌우)가 이 값 이하면 무시 — 숨쉬기·걷기 보빙 같은 잔떨림 제거")]
    [SerializeField] float animDeadzone = 1.5f;
    [Tooltip("가속도 노이즈 필터 (클수록 반응 빠름)")]
    [SerializeField] float accelSmoothing = 20f;

    [Header("Input (Hips 비틀림)")]
    [Tooltip("Hips가 비틀리는(Yaw) 각가속도 1 deg/s² 당 척추 체인의 반대 방향 비틀림(도). 0이면 끔")]
    [SerializeField] float degreesPerYawAccel = 0.004f;
    [SerializeField] float maxYawAngle = 15f;
    [Tooltip("Hips 각가속도(deg/s²)가 이 값 이하면 무시")]
    [SerializeField] float yawDeadzone = 300f;

    Transform hips;
    Vector3 prevHipsOffset, prevHipsVel, hipsAccFiltered; // Hips의 루트 기준 위치(월드 스케일, 루트 방향 좌표)
    float prevHipsYaw, prevYawVel, yawAccFiltered;         // Hips의 루트 기준 Yaw(도)
    Vector2 angle, angVel;     // x = 앞뒤(pitch), y = 좌우(roll)
    float yawAngle, yawAngVel; // 비틀림(루트 up 축)

    void Awake()
    {
        AutoFillChain();
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.isHuman) hips = animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    void OnEnable() => ResetState();

    void AutoFillChain()
    {
        if (chain != null && chain.Length > 0) return;
        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman) return;

        var list = new List<Joint>();
        AddBone(list, animator, HumanBodyBones.Spine, 0.3f);
        // Chest가 없는 리그는 UpperChest로 대체
        if (!AddBone(list, animator, HumanBodyBones.Chest, 0.3f))
            AddBone(list, animator, HumanBodyBones.UpperChest, 0.3f);
        AddBone(list, animator, HumanBodyBones.Neck, 0.2f);
        AddBone(list, animator, HumanBodyBones.Head, 0.2f);
        chain = list.ToArray();
    }

    static bool AddBone(List<Joint> list, Animator animator, HumanBodyBones id, float weight)
    {
        Transform bone = animator.GetBoneTransform(id);
        if (bone == null) return false;
        list.Add(new Joint { bone = bone, weight = weight });
        return true;
    }

    public void ResetState()
    {
        if (root == null) root = transform;
        prevHipsOffset = HipsOffset();
        prevHipsVel = hipsAccFiltered = Vector3.zero;
        prevHipsYaw = HipsYaw();
        prevYawVel = yawAccFiltered = 0f;
        angle = angVel = Vector2.zero;
        yawAngle = yawAngVel = 0f;
    }

    // Hips의 루트 기준 위치. 월드 이동(DOMove)은 빠지고 애니메이션이 만든 몸통 움직임만 남는다.
    Vector3 HipsOffset() => hips != null ? root.InverseTransformDirection(hips.position - root.position) : Vector3.zero;

    // Hips가 루트 정면에 대해 얼마나 돌아가 있는지(도). 루트 자체의 회전(LookRotation 등)은 빠진다.
    float HipsYaw()
    {
        if (hips == null) return 0f;
        Vector3 f = root.InverseTransformDirection(hips.forward);
        return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || hips == null) return;

        float filter = 1f - Mathf.Exp(-accelSmoothing * dt);

        // 1) Hips 가속도(루트 기준). 수직(y)은 걷기 보빙이라 제외, 앞뒤·좌우만 데드존을 거쳐 쓴다.
        Vector2 target = Vector2.zero;
        if (animGain > 0f)
        {
            Vector3 hipsOffset = HipsOffset();
            Vector3 hipsVel = (hipsOffset - prevHipsOffset) / dt;
            Vector3 hipsAcc = (hipsVel - prevHipsVel) / dt;
            prevHipsOffset = hipsOffset;
            prevHipsVel = hipsVel;
            hipsAccFiltered = Vector3.Lerp(hipsAccFiltered, hipsAcc, filter);

            Vector2 flat = new Vector2(hipsAccFiltered.x, hipsAccFiltered.z);
            float mag = flat.magnitude;
            if (mag > animDeadzone)
            {
                flat *= (mag - animDeadzone) / mag * animGain;
                // 몸이 뒤로 가속(=감속)하면 상체는 앞으로 쏠림, 오른쪽 가속 → 왼쪽으로 기울어짐
                target = Vector2.ClampMagnitude(new Vector2(-flat.y, flat.x) * degreesPerAccel, maxAngle);
            }
        }

        // 2) 감쇠 스프링 (semi-implicit Euler). 입력이 사라지면 오버슈트하며 0으로 복귀 = "멈춰도 출렁임"
        float w = 2f * Mathf.PI * frequency;
        Vector2 a = w * w * (target - angle) - 2f * damping * w * angVel;
        angVel += a * dt;
        angle = Vector2.ClampMagnitude(angle + angVel * dt, maxAngle);

        // 3) Hips 비틀림(Yaw) 각가속도 → 같은 스프링. 하체가 먼저 돌면 상체는 관성으로 늦게 따라오므로 반대 부호.
        float yawTarget = 0f;
        if (degreesPerYawAccel > 0f)
        {
            float yaw = HipsYaw();
            float yawVel = Mathf.DeltaAngle(prevHipsYaw, yaw) / dt;
            float yawAcc = (yawVel - prevYawVel) / dt;
            prevHipsYaw = yaw;
            prevYawVel = yawVel;
            yawAccFiltered = Mathf.Lerp(yawAccFiltered, yawAcc, filter);

            float mag = Mathf.Abs(yawAccFiltered);
            if (mag > yawDeadzone)
                yawTarget = Mathf.Clamp(-Mathf.Sign(yawAccFiltered) * (mag - yawDeadzone) * degreesPerYawAccel, -maxYawAngle, maxYawAngle);
        }
        float yawA = w * w * (yawTarget - yawAngle) - 2f * damping * w * yawAngVel;
        yawAngVel += yawA * dt;
        yawAngle = Mathf.Clamp(yawAngle + yawAngVel * dt, -maxYawAngle, maxYawAngle);

        // 4) 애니메이션 결과 위에 회전 덧입히기
        foreach (var j in chain)
        {
            if (j.bone == null || j.weight <= 0f) continue;
            Quaternion offset =
                Quaternion.AngleAxis(angle.x * j.weight, root.right) *
                Quaternion.AngleAxis(angle.y * j.weight, root.forward) *
                Quaternion.AngleAxis(yawAngle * j.weight, root.up);
            j.bone.rotation = offset * j.bone.rotation;
        }
    }
}
