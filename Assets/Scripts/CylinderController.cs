using UnityEngine;

public class CylinderController : MonoBehaviour
{
    // [방법 1] 인스펙터에서 센서를 드래그하여 할당 (이건 보통 잘 됩니다)
    public OpticalSensor targetSensor;

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        // 오브젝트가 활성화될 때 구독 시작
        if (targetSensor != null)
        {
            // 센서의 OnDetected 이벤트에 내 함수를 등록 (구독)
            targetSensor.OnDetected.AddListener(PushCylinder);
        }
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화될 때 구독 해제 (메모리 누수 및 에러 방지)
        if (targetSensor != null)
        {
            targetSensor.OnDetected.RemoveListener(PushCylinder);
        }
    }

    public void PushCylinder()
    {
        //Debug.Log($"doPush {animator != null}");
        if (animator != null)
        {
            animator.SetTrigger("doPush");
            //Debug.Log($"{gameObject.name}: 센서 신호를 받아 작동합니다.");
        }
    }
}