using UnityEngine;
using UnityEngine.Events;

public class SlotSensor : MonoBehaviour
{
    [Header("상태 모니터링")]
    public bool isDetected = false; // 현재 감지 여부
    private int triggerCount = 0;   // 영역 내에 있는 물체 개수

    [Header("이벤트")]
    public UnityEvent OnDetected;    // 감지 시 실행

    private void OnTriggerEnter(Collider other)
    {
        // 영역 내에 물체가 하나도 없다가 처음 들어왔을 때 (최초 감지)
        if (triggerCount == 0)
        {
            OnDetected.Invoke();
            isDetected = true;
            Debug.Log($" {gameObject.name}: ON");
        }

        // 영역 내 물체 수 증가
        triggerCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        // 영역 내 물체 수 감소
        triggerCount--;

        // 영역 내에 물체가 더 이상 없을 때 (감지 해제)
        if (triggerCount <= 0)
        {
            triggerCount = 0; // 마이너스 방지
            isDetected = false;
            Debug.Log($"{gameObject.name}: OFF");
        }
    }
}