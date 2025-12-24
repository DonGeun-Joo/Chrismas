using UnityEngine;
using System.Collections;

public class MainControlSystem : MonoBehaviour
{
    [Header("1. 공급단 센서 및 푸셔")]
    public OpticalSensor supplyCanSensor;
    public OpticalSensor supplyBarrelSensor;
    public CylinderController canPusher;
    public CylinderController barrelPusher;

    [Header("2. 컨베이어 끝단 센서")]
    public OpticalSensor endCanSensor;
    public OpticalSensor endBarrelSensor;

    [Header("3. 로봇 제어")]
    public URCommandSender urSender;

    [Header("상태 모니터링 (Read Only)")]
    public bool _needPickCan = false;
    public bool _needPickBarrel = false;
    public bool isRobotBusy = false; // 로봇이 작업 중인지 확인

    private bool pickOrderToggle = true; // Can(true)과 Barrel(false) 번갈아 확인용

    void Update()
    {
        // --- [Part 1: 푸셔 제어 로직] ---
        // 공급단에 Can이 감지되면 푸셔 전진
        if (supplyCanSensor != null && supplyCanSensor.GetDetectedState())
        {
            canPusher.PushCylinder();
        }
        // 공급단에 Barrel이 감지되면 푸셔 전진
        if (supplyBarrelSensor != null && supplyBarrelSensor.GetDetectedState())
        {
            barrelPusher.PushCylinder();
        }

        // --- [Part 2: 피킹 플래그 관리] ---
        // Can이 끝에 도달했는가?
        if (endCanSensor != null && endCanSensor.GetDetectedState())
        {
            _needPickCan = true;
        }
        // Barrel이 끝에 도달했는가?
        if (endBarrelSensor != null && endBarrelSensor.GetDetectedState())
        {
            _needPickBarrel = true;
        }

        // --- [Part 3: 로봇 피킹 스케줄러] ---
        if (!isRobotBusy)
        {
            ScheduleRobotTask();
        }
    }

    void ScheduleRobotTask()
    {
        // 두 제품이 모두 기다리고 있다면 번갈아 가며 작업
        if (_needPickCan && _needPickBarrel)
        {
            if (pickOrderToggle) ExecutePickCan();
            else ExecutePickBarrel();

            pickOrderToggle = !pickOrderToggle; // 다음 순서 변경
        }
        // 하나만 기다리고 있다면 즉시 실행
        else if (_needPickCan)
        {
            ExecutePickCan();
        }
        else if (_needPickBarrel)
        {
            ExecutePickBarrel();
        }
    }

    void ExecutePickCan()
    {
        isRobotBusy = true;
        _needPickCan = false; // 플래그 리셋
        urSender.SendPickCommand("CAN"); // UR에 Can 피킹 스크립트 전송
        StartCoroutine(ResetRobotBusy(5f)); // 로봇 작업 시간(예: 5초) 동안 대기
    }

    void ExecutePickBarrel()
    {
        isRobotBusy = true;
        _needPickBarrel = false; // 플래그 리셋
        urSender.SendPickCommand("BARREL"); // UR에 Barrel 피킹 스크립트 전송
        StartCoroutine(ResetRobotBusy(5f));
    }

    IEnumerator ResetRobotBusy(float delay)
    {
        yield return new WaitForSeconds(delay);
        isRobotBusy = false;
        Debug.Log("로봇 작업 완료 - 다음 작업 가능");
    }
}