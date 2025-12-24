using UnityEngine;
using UnityEngine.UI; // 버튼 텍스트 변경을 위해 필요

// 아진엑스텍 서보 온/오프 토글 버튼 스크립트
public class ServoToggleButton : MonoBehaviour
{
    [Header("Target Axis")]
    public int axisNo = 0; // 제어할 축 번호

    [Header("UI Feedback")]
    public Text buttonText; // 버튼 안의 텍스트 (상태 표시용)

    // 버튼 클릭 시 호출될 함수
    public void ToggleServoState()
    {
        if (AjinextekManager.Instance == null) { Debug.LogError("AjinextekManager가 없습니다!"); return; }

        uint isOn = 0;
        // 1. 현재 서보 상태 확인 (isOn이 1이면 ON, 0이면 OFF)
        CAXM.AxmSignalIsServoOn(axisNo, ref isOn);

        uint ret = 0;
        if (isOn == 1)
        {
            // 2. 켜져 있으면 -> 끕니다 (OFF 호출)
            ret = CAXM.AxmSignalServoOn(axisNo, 0); // 0: OFF
            if (ret == 0) Debug.Log($"Axis {axisNo}: Servo OFF Requested");
        }
        else
        {
            // 2. 꺼져 있으면 -> 켭니다 (ON 호출)
            ret = CAXM.AxmSignalServoOn(axisNo, 1); // 1: ON
            if (ret == 0) Debug.Log($"Axis {axisNo}: Servo ON Requested");
        }

        if (ret != 0) Debug.LogError($"Servo Toggle Error: {ret}");

        // (선택사항) 버튼 텍스트 즉시 업데이트 (실제 반영엔 딜레이가 있을 수 있음)
        UpdateButtonText(isOn == 0);
    }

    // 버튼 텍스트를 상태에 맞춰 바꿔주는 보조 함수
    private void UpdateButtonText(bool isTurningOn)
    {
        if (buttonText != null)
        {
            buttonText.text = isTurningOn ? "Servo ON" : "Servo OFF";
        }
    }
}