using UnityEngine;

public class ReleaseRotation : MonoBehaviour
{
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // 방식 A: Limit 오브젝트가 'Is Trigger'로 설정된 경우 (추천)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Limit"))
        {
            UnfreezeRotation();
        }
    }

    private void UnfreezeRotation()
    {
        if (_rb != null)
        {
            // Freeze Rotation (X, Y, Z)를 모두 해제합니다.
            // 만약 Freeze Position도 해제하고 싶다면 RigidbodyConstraints.None을 사용하세요.
            _rb.constraints &= ~RigidbodyConstraints.FreezeRotation;

            //Debug.Log($"{gameObject.name}: 회전 고정이 해제되었습니다.");
        }
    }
}