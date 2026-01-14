using UnityEngine;

public class PLC_InputAdapter : MonoBehaviour
{
    [Header("PLC Address Settings")]
    public string plcAddress; // PLC의 입력 주소 (예: "X10")

    void Start()
    {
        // 1. 광센서(OpticalSensor) 연결 확인
        var optical = GetComponent<OpticalSensor>();
        if (optical != null)
        {
            optical.OnDetected.AddListener(() => SendToManager(true));
            optical.OnLost.AddListener(() => SendToManager(false));
        }

        // 2. 오토스위치(AutoSwitch) 연결 확인
        var autoSwitch = GetComponent<AutoSwitch>();
        if (autoSwitch != null)
        {
            autoSwitch.OnDetected.AddListener(() => SendToManager(true));
            autoSwitch.OnLost.AddListener(() => SendToManager(false));
        }

        // 3. 푸시 버튼(Push_Button) 연결 확인
        var pushButton = GetComponent<Push_Button>();
        if (pushButton != null)
        {
            pushButton.OnPressed.AddListener(() => SendToManager(true));
            pushButton.OnReleased.AddListener(() => SendToManager(false));
        }

        // 4. 로봇 브릿지(UR_ModbusBridge) 연결 확인
        var robotBridge = GetComponent<UR_ModbusBridge>();
        if (robotBridge != null)
        {
            // 로봇의 Busy 상태 변화 이벤트를 구독
            robotBridge.OnRobotBusyChanged += (isOn) => SendToManager(isOn);
        }

        
    }

    private void SendToManager(bool isOn)
    {
        if (IO_Manager.Instance != null && !string.IsNullOrEmpty(plcAddress))
        {
            // 로봇/센서의 상태를 PLC의 X 주소로 전달
            IO_Manager.Instance.SetOutput(plcAddress, isOn);
            // Debug.Log($"[PLC Adapter] {plcAddress} -> {isOn}");
        }
        else if (IO_Manager.Instance == null)
        {
            Debug.LogWarning("IO_Manager Instance is missing!");
        }
    }
}