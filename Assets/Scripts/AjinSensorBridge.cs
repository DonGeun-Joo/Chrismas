using UnityEngine;

public class AjinSensorBridge : MonoBehaviour
{
    public enum SensorType { Home = 1, PositiveLimit = 2, NegativeLimit = 3 }

    [Header("아진 설정")]
    public int axisNo = 0;              // 제어할 축 번호
    public SensorType sensorType;       // 이 센서의 종류 선택

    // OpticalSensor의 OnDetected() 이벤트에 이 함수를 연결하세요.
    public void OnSensorDetected()
    {
        /*if (AjinextekManager.Instance == null) return;

        if (sensorType == SensorType.PositiveLimit ||
            sensorType == SensorType.NegativeLimit)
        {
            // 1. 리밋 센서인 경우: 즉시 모터 급정지 명령
            CAXM.AxmMoveEStop(axisNo);
            Debug.Log($"<color=red>[Limit Hit]</color> 축 {axisNo} 리밋 감지: 급정지 호출");
        }*/
        /*else if (sensorType == SensorType.Home)
        {
            // 2. 홈 센서인 경우: 현재 위치를 0으로 선언 (원점 잡기)
            CAXM.AxmMoveSStop(axisNo); // 일단 멈춤
            CAXM.AxmStatusSetPosMatch(axisNo, 0.0);
            Debug.Log("<color=yellow>[Home Hit]</color> 홈 센서 감지: 좌표 0으로 초기화");
        }*/
    }
}