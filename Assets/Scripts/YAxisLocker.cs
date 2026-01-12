using UnityEngine;

public class YAxisLocker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. 충돌한 물체에 Rigidbody가 있는지 확인
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 2. 기존의 제약 조건(Constraints)을 유지하면서 Z축 이동만 잠금
            // 비트 연산자(|=)를 사용하여 기존 설정을 보존합니다.
            rb.constraints |= RigidbodyConstraints.FreezePositionY;

            Debug.Log($"{other.name}의 Z축 이동이 잠겼습니다.");
        }
    }
}