using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class MotorControlPanel : MonoBehaviour
{
    [Serializable]
    public class PresetPosition
    {
        public string label;
        public double position;
        public double velocity;
    }

    [Header("PLC Address Settings")]
    public string commandAddress = "D128";  // 명령 수신 (1~4)
    public string statusAddress = "D129";   // 상태 전송 (이동중: 1, 정지: 0)
                                            // 만약 X 주소를 쓰고 싶다면 "X20" 등으로 변경 가능합니다.

    [Header("Preset Positions")]
    public List<PresetPosition> presetList = new List<PresetPosition>();

    [Header("Axis Settings")]
    public int axisNo = 0;
    public double defaultAcc = 20000;
    public double defaultDec = 20000;

    [Header("Input Fields (TMP)")]
    public TMP_InputField inputTargetPos;
    public TMP_InputField inputTargetVel;
    public TMP_InputField inputAcc;
    public TMP_InputField inputDec;

    [Header("Display Text (TMP)")]
    public TextMeshProUGUI txtActualPos;

    private short _lastCommandValue = -1;
    private bool _isCurrentlyMoving = false; // 현재 이동 상태 저장

    private void Start()
    {
        inputAcc.text = defaultAcc.ToString();
        inputDec.text = defaultDec.ToString();

        if (presetList.Count == 0)
        {
            for (int i = 0; i < 4; i++) presetList.Add(new PresetPosition { label = $"Position {i + 1}" });
        }
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

        if (currentVal != _lastCommandValue)
        {
            if (currentVal >= 1 && currentVal <= presetList.Count)
            {
                ExecutePresetMove(currentVal - 1);
            }
            _lastCommandValue = currentVal;
        }
    }

    // --- [모터 상태 보고: D129] ---
    private void UpdateMovingStatus()
    {
        // AxmStatusReadStatus: 축의 상태를 읽어옴
        uint uStatus = CAXM.AxmInfoGetAxisStatus(axisNo); ;

        // 0번 비트가 1이면 모터가 현재 구동 중(Busy)임을 의미함
        bool isMoving = (uStatus & 0x01) != 0;

        // 상태가 변경되었을 때만 PLC에 데이터 전송 (통신 부하 감소)
        if (isMoving != _isCurrentlyMoving)
        {
            _isCurrentlyMoving = isMoving;
            short statusVal = (short)(isMoving ? 1 : 0);

            // D129에 상태 기록
            IO_Manager.Instance.SetRegister(statusAddress, statusVal);

            // 만약 X 주소를 사용하고 싶다면 아래 주석을 해제하세요.
            // IO_Manager.Instance.SetOutput("X20", isMoving);

            Debug.Log($"[Motor Status] {axisNo}번 축 이동 상태 변경: {isMoving} (PLC {statusAddress}에 {statusVal} 기록)");
        }
    }

    private void ExecutePresetMove(int index)
    {
        PresetPosition target = presetList[index];
        double acc = Convert.ToDouble(inputAcc.text);
        double dec = Convert.ToDouble(inputDec.text);

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