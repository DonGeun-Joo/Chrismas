using UnityEngine;

// 이름 충돌 방지를 위해 클래스명 변경
public class PLC_InputAdapter : MonoBehaviour
{
    public string plcAddress; // PLC의 입력 주소 (이미지 리스트 기반: 예 "X6")
    private OpticalSensor _sensor;

    void Start()
    {
        // 광센서가 있으면 연결
        _sensor = GetComponent<OpticalSensor>();
        if (_sensor != null)
        {
            _sensor.OnDetected.AddListener(() => SendToManager(true));
            _sensor.OnLost.AddListener(() => SendToManager(false));
        }

        // 오토스위치가 있으면 연결
        var autoSwitch = GetComponent<AutoSwitch>();
        if (autoSwitch != null)
        {
            autoSwitch.OnDetected.AddListener(() => SendToManager(true));
            autoSwitch.OnLost.AddListener(() => SendToManager(false));
        }

        // 푸시 버튼이 있으면 연결
        var pushButton = GetComponent<Push_Button>();
        if (pushButton != null)
        {
            pushButton.OnPressed.AddListener(() => SendToManager(true));
            pushButton.OnReleased.AddListener(() => SendToManager(false));
        }
    }

    private void SendToManager(bool isOn)
    {
        // 이제 직접 통신하지 않고 IO_Manager에게 전달합니다.
        if (IO_Manager.Instance != null)
        {
            IO_Manager.Instance.SetOutput(plcAddress, isOn);
        }
        else
        {
            Debug.LogWarning("IO_Manager 인스턴스를 찾을 수 없습니다.");
        }
    }
}