using UnityEngine;

public class CylinderController : MonoBehaviour
{
    [Header("PLC 연동 설정")]
    public PLC_OutputAdapter plcOutput; // 실린더를 움직일 출력 신호 (예: Y20)

    [Header("물리 설정")]
    public Rigidbody rodRigidbody;      // 실린더 로드의 리지드바디
    public float moveSpeed = 2.0f;      // 실린더 이동 속도
    public Vector3 moveAxis = Vector3.forward; // 전진 방향 (로컬 축)

    public bool showLog = false;

    void Start()
    {
        if (plcOutput == null) plcOutput = GetComponent<PLC_OutputAdapter>();

        // 리지드바디 설정 최적화
        if (rodRigidbody != null)
        {
            rodRigidbody.useGravity = false;
            rodRigidbody.isKinematic = false;
            rodRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
    void FixedUpdate()
    {
        if (plcOutput == null || rodRigidbody == null) return;

        // 1. PLC 출력 신호(Y)에 따른 방향 결정 (1: 전진, -1: 후진)
        float directionMultiplier = plcOutput.IsOn ? 1f : -1f;

        // 2. Rod의 로컬 X축(1,0,0) 방향을 월드 좌표로 변환
        // 현재 상황: 이 결과값이 월드의 Z축(0,0,1) 부근으로 계산됩니다.
        Vector3 worldDirection = rodRigidbody.transform.rotation * moveAxis.normalized;

        // 3. 물리 속도 적용 (linearVelocity는 월드 기준)
        // 이 속도는 월드 Z축 방향으로 가해지며, 리지드바디의 Freeze Z가 풀려있어야 움직입니다.
        rodRigidbody.linearVelocity = worldDirection * directionMultiplier * moveSpeed;

        if (showLog)
        {
            Debug.Log($"{plcOutput.plcAddress} | 로컬방향:{moveAxis} -> 월드방향:{worldDirection} | 실제속도:{rodRigidbody.linearVelocity}");
        }
    }

}