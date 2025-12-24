using UnityEngine;
using UnityEngine.Events;

public class LimitSensor : MonoBehaviour
{
    [Header("감지 대상 설정")]
    [Tooltip("이 센서가 감지할 실린더의 가동부(Rod)를 드래그하세요.")]
    public GameObject targetRod;

    [Header("이벤트")]
    public UnityEvent OnLimitReached; // 센서 도달 시 실행할 동작

    private void OnTriggerEnter(Collider other)
    {
        // 1. 타겟이 설정되어 있지 않으면 무시
        if (targetRod == null) return;

        // 2. 충돌한 오브젝트(other)가 우리가 드래그해 넣은 targetRod인지 비교
        // (Tip: 실린더 가동부에 콜라이더가 여러 개일 수 있으므로 
        //  충돌한 물체 본인 혹은 그 부모가 targetRod인지 확인하는 것이 가장 안전합니다.)
        if (other.gameObject == targetRod || other.transform.IsChildOf(targetRod.transform))
        {
            OnLimitReached.Invoke();
            //Debug.Log($"{gameObject.name}: {targetRod.name} 감지 완료!");
        }
    }
}