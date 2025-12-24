using UnityEngine;
using DG.Tweening;
using System.Linq; // 배열 변환을 위해 필요

public class AbsoluteRobotMover : MonoBehaviour
{
    [Header("1. 연결 대상")]
    public Transform ikTarget;
    public Transform[] waypoints;

    [Header("2. 바닥을 보는 각도")]
    public Vector3 floorFacingAngle = new Vector3(180, 0, 0);

    [Header("3. Movel 설정")]
    public float totalDuration = 5.0f; // 전체 경로를 도는 데 걸리는 시간
    public Ease moveEase = Ease.Linear; // Linear로 해야 기계적인 등속 운동을 함

    void Start()
    {
        StartLinearMove();
    }

    void Update()
    {
        // 회전은 여전히 바닥을 보도록 강제 고정
        if (ikTarget != null)
        {
            ikTarget.rotation = Quaternion.Euler(floorFacingAngle);
        }
    }

    void StartLinearMove()
    {
        // 1. Transform 배열을 Vector3(위치) 배열로 변환
        // (DOPath는 Transform이 아니라 좌표값 목록이 필요합니다)
        Vector3[] pathPositions = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            pathPositions[i] = waypoints[i].position;
        }

        // 2. DOPath 실행 (핵심)
        // PathType.Linear: 점과 점 사이를 자를 대고 그은 듯 직선으로 연결
        // PathMode.Full3D: 3차원 공간 이동
        ikTarget.DOPath(pathPositions, totalDuration, PathType.Linear, PathMode.Full3D)
            .SetEase(moveEase) // Ease.Linear를 쓰면 가속/감속 없이 등속도 이동
            .SetOptions(true) // true: 경로가 닫힘(마지막 점 -> 첫 점 연결), false: 연결 안 함
            .SetLoops(-1, LoopType.Restart); // 무한 반복
    }
}