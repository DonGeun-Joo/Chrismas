using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro; // TextMeshPro 사용을 위해 필수

public class MotorControlPanel : MonoBehaviour
{
    [Header("Axis Settings")]
    public int axisNo = 0;
    public double defaultAcc = 20000;
    public double defaultDec = 20000;

    [Header("Input Fields (TMP)")]
    public TMP_InputField inputTargetPos;  // 목표 위치
    public TMP_InputField inputTargetVel;  // 이동 속도
    public TMP_InputField inputAcc;  // 이동 속도
    public TMP_InputField inputDec;  // 이동 속도

    [Header("Display Text (TMP)")]
    public TextMeshProUGUI txtActualPos;   // 현재 위치 표시

    private void Start()
    {
        inputAcc.text = defaultAcc.ToString();
        inputDec.text = defaultDec.ToString();
    }
    void Update()
    {
        if (AjinextekManager.Instance == null) return;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        double curPos = 0;
        CAXM.AxmStatusGetActPos(axisNo, ref curPos);
        txtActualPos.text = curPos.ToString("F3");
    }

    // --- [버튼 연결용 함수] ---

    // 1. 절대위치 이동 버튼 (Absolute Move)
    public void OnClickAbsMove()
    {
        if (AjinextekManager.Instance == null) return;

        // 입력창이 비어있는지 확인 (방어 코드)
        if (string.IsNullOrEmpty(inputTargetPos.text) || string.IsNullOrEmpty(inputTargetVel.text))
        {
            Debug.LogWarning("목표 위치와 속도를 모두 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(inputAcc.text) || string.IsNullOrEmpty(inputDec.text))
        {
            Debug.LogWarning("가속도와 감속도를 모두 입력해주세요.");
            return;
        }

        try
        {
            // 문자열을 숫자로 변환
            double pos = Convert.ToDouble(inputTargetPos.text);
            double vel = Convert.ToDouble(inputTargetVel.text);
            double acc = Convert.ToDouble(inputAcc.text);
            double dec = Convert.ToDouble(inputDec.text);

            // 아진 API 호출: 지정된 절대 좌표로 이동 시작
            uint ret = CAXM.AxmMoveStartPos(axisNo, pos, vel, acc, dec);

            if (ret != 0) Debug.LogError($"절대이동 시작 실패: {ret}");
        }
        catch (Exception e)
        {
            Debug.LogError($"입력값 형식이 잘못되었습니다: {e.Message}");
        }
    }

    // 2. 스탑 버튼 (Stop - 감속 정지)
    public void OnClickStop()
    {
        if (AjinextekManager.Instance == null) return;

        // AxmMoveSStop: 현재 설정된 감속도(Deceleration)를 사용하여 부드럽게 정지
        uint ret = CAXM.AxmHomeSetResult(axisNo, 0); // 홈 복귀 중이었다면 결과 리셋
        ret = CAXM.AxmMoveSStop(axisNo);

        if (ret == 0) Debug.Log($"축 {axisNo}: 감속 정지 명령 전송");
        else Debug.LogError($"정지 명령 실패: {ret}");
    }

    // 3. 비상 정지 버튼 (Emergency Stop - 즉시 정지)
    // 필요하시다면 별도의 빨간 버튼에 연결해서 사용하세요.
    public void OnClickEStop()
    {
        if (AjinextekManager.Instance == null) return;

        uint ret = CAXM.AxmMoveEStop(axisNo); // 즉시 정지
        Debug.LogWarning($"축 {axisNo}: 비상 정지(E-STOP) 실행!");
    }
}