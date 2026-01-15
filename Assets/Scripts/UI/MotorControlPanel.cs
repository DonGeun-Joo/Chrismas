using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class MotorControlPanel : MonoBehaviour
{
    public enum CV
    {
        Blue = 2,
        Red,
        Can,
        Wood
    }


    [Serializable]
    public class PresetPosition
    {
        public string label;
        public double position;
        public double velocity = 100000;
    }

    [Header("PLC Address Settings")]
    public string commandAddress = "D128";  // 명령 수신 (1~4)
    public string statusAddress = "D129";   // 상태 전송 (이동중: 1, 정지: 0)
                                            // 만약 X 주소를 쓰고 싶다면 "X20" 등으로 변경 가능합니다.

    [Header("Preset Positions")]
    public PresetPosition bluePosition = new PresetPosition();
    public PresetPosition redPosition = new PresetPosition();
    public PresetPosition canPosition = new PresetPosition();
    public PresetPosition woodPosition = new PresetPosition();

    [Header("Axis Settings")]
    public int axisNo = 0;
    public double defaultAcc = 100000;
    public double defaultDec = 100000;
    public double defaultSpeed = 200000;

    [Header("Input Fields (TMP)")]
    public TMP_InputField inputTargetPos;
    public TMP_InputField inputTargetVel;
    public TMP_InputField inputAcc;
    public TMP_InputField inputDec;

    [Header("Display Text (TMP)")]
    public TextMeshProUGUI txtActualPos;

    public string robotBusyAddress;

    private short _lastCommandValue = -1;
    private bool _isCurrentlyMoving = false; // 현재 이동 상태 저장
    private short _moterStatus = -1;

    private void Start()
    {
        inputAcc.text = defaultAcc.ToString();
        inputDec.text = defaultDec.ToString();
        inputTargetVel.text = defaultSpeed.ToString();

    }

    void Update()
    {
        if (AjinextekManager.Instance == null || IO_Manager.Instance == null) return;

        UpdateStatus();      // 1. 화면 UI 갱신 (현재 위치)
        CheckPlcTrigger();   // 2. PLC 명령 확인 (D128)
        UpdateMovingStatus(); // 3. 모터 이동 상태 확인 및 PLC 보고 (D129)
    }

    private void UpdateStatus()
    {
        double curPos = 0;
        CAXM.AxmStatusGetActPos(axisNo, ref curPos);
        txtActualPos.text = curPos.ToString("F3");
    }

    // --- [PLC 명령 감시: D128] ---
    private void CheckPlcTrigger()
    {
        short currentVal = IO_Manager.Instance.GetRegister(commandAddress);

        // 1. 명령 주소의 값이 변경되었는지 확인
        if (currentVal != _lastCommandValue)
        {
            if (currentVal >= 1)
            {
                // 2. 인터락 체크 로직 추가
                if (CheckReadyToMove())
                {
                    // 이동 실행 (기존 로직: 1이면 index -1, 2이면 index 0...)
                    ExecutePresetMove((CV)currentVal);
                }
                else
                {
                    Debug.LogWarning($"<color=red>[Motor Alert]</color> 명령({currentVal})이 수신되었으나, 인터락 신호({robotBusyAddress})가 꺼져 있어 무시되었습니다.");
                }
            }
            _lastCommandValue = currentVal;
        }
    }

    /// <summary>
    /// 모터가 움직여도 되는 안전 상태인지 확인합니다.
    /// </summary>
    private bool CheckReadyToMove()
    {

        // IO_Manager를 통해 특정 입력 접점(X) 상태 확인
        return !IO_Manager.Instance.GetBit(robotBusyAddress);
    }

    public short GetMotionStatus()
    {
        return _moterStatus;
    }

    // --- [모터 상태 보고: D129] ---
    private void UpdateMovingStatus()
    {
        uint uInMotion = 0;

        // AxmStatusReadInMotion: 축이 이동 중인지 여부를 직접 읽어옴
        // 반환값(ret): 0이면 성공, 그 외에는 에러
        // uInMotion: 1이면 이동 중(Busy), 0이면 정지 상태
        uint ret = CAXM.AxmStatusReadInMotion(axisNo, ref uInMotion);

        if (ret != 0)
        {
            // API 호출 실패 시 로그 출력 (필요 시 주석 해제)
            // Debug.LogError($"AxmStatusReadInMotion 에러: {ret}");
            return;
        }

        bool isMoving = (uInMotion == 1);

        // 상태가 변경되었을 때만 PLC에 데이터 전송 (통신 부하 감소)
        if (isMoving != _isCurrentlyMoving)
        {
            _isCurrentlyMoving = isMoving;
            _moterStatus = (short)(isMoving ? 1 : 0);

            // D129에 상태 기록
            if (IO_Manager.Instance != null)
            {
                IO_Manager.Instance.SetRegister(statusAddress, _moterStatus);

                // 디버그 로그: 실제 현장에서 상태 변화를 확인하기 위함
                Debug.Log($"<color=cyan>[Motor Status]</color> {axisNo}번 축 상태 변경 -> {(isMoving ? "이동 중 (1)" : "정지 (0)")}");
            }
        }
    }

    private void ExecutePresetMove(CV cvNum)
    {
        PresetPosition target = new PresetPosition();
        switch (cvNum)
        {
            case CV.Blue:
                target = bluePosition;
                break;

            case CV.Red:
                target = redPosition;
                break;

            case CV.Can:
                target = canPosition;
                break;

            case CV.Wood:
                target = woodPosition;
                break;

        }
        double acc = Convert.ToDouble(inputAcc.text);
        double dec = Convert.ToDouble(inputDec.text);
        _moterStatus = 1;
        uint ret = CAXM.AxmMoveStartPos(axisNo, target.position, target.velocity, acc, dec);
        if (ret != 0) Debug.LogError($"프리셋 이동 실패: {ret}");
    }

    // --- [UI 버튼용 함수들] ---
    public void OnClickAbsMove()
    {
        try
        {
            double pos = Convert.ToDouble(inputTargetPos.text);
            double vel = Convert.ToDouble(inputTargetVel.text);
            double acc = Convert.ToDouble(inputAcc.text);
            double dec = Convert.ToDouble(inputDec.text);
            CAXM.AxmMoveStartPos(axisNo, pos, vel, acc, dec);
        }
        catch (Exception e) { Debug.LogError($"입력값 오류: {e.Message}"); }
    }

    public void OnClickStop() { CAXM.AxmMoveSStop(axisNo); }
    public void OnClickEStop() { CAXM.AxmMoveEStop(axisNo); }
}