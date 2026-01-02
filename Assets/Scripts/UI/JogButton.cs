using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; // 마우스 클릭/터치 이벤트를 위해 필수

// 아진엑스텍 조그(속도) 이동 버튼 스크립트
public class JogButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Motion Settings")]
    public int axisNo = 0;           // 제어할 축 번호 (보통 0번부터 시작)
    public double velocity = 10000;  // 조그 속도 (단위: pulse/sec 또는 mm/sec)
    public double accel = 20000;     // 가속도
    public double decel = 20000;     // 감속도

    [Header("Input Fields (TMP)")]
    public TMP_InputField inputSpeed;  // 속도
    public TMP_InputField inputAcc;  // 가속
    public TMP_InputField inputDec;  // 감속


    [Header("Direction")]
    [Tooltip("체크하면 (+), 해제하면 (-) 방향으로 이동합니다.")]
    public bool isPositive = true;   // 방향 설정



    // 버튼을 누르는 순간 호출됩니다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (AjinextekManager.Instance == null) return;

        // 1. 서보가 켜져 있는지 먼저 확인합니다.
        uint servoOn = 0;
        CAXM.AxmSignalIsServoOn(axisNo, ref servoOn);

        if (servoOn == 0) // 0은 OFF 상태
        {
            Debug.LogWarning($"축 {axisNo}의 서보가 꺼져 있어 조그를 시작할 수 없습니다. Servo ON을 먼저 눌러주세요.");
            return; // 서보가 꺼져 있으면 명령을 보내지 않고 종료
        }

        // 2. 알람 상태인지도 확인하면 더 좋습니다.
        uint alarmStatus = 0;
        CAXM.AxmSignalReadServoAlarm(axisNo, ref alarmStatus);
        if (alarmStatus != 0)
        {
            Debug.LogError($"축 {axisNo}에 알람이 발생했습니다. 알람을 먼저 리셋해야 합니다.");
            return;
        }

        // 3. 모든 상태가 정상이면 명령 실행
        double targetVel = isPositive ? System.Math.Abs(velocity) : -System.Math.Abs(velocity);
        uint ret = CAXM.AxmMoveVel(axisNo, targetVel, accel, decel);

        if (ret != 0 && ret != 4152) // 4152 에러가 여전히 난다면 로그만 출력
        {
            Debug.LogError($"Jog Start Error: {ret}");
        }
    }

    // 버튼에서 손을 떼는 순간 호출됩니다.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (AjinextekManager.Instance == null) return;

        // 아진 API: 감속 정지
        CAXM.AxmMoveSStop(axisNo);
        //Debug.Log($"Jog Stop: Axis {axisNo}");
    }
}