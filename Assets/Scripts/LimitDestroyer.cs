using UnityEngine;

public class LimitDestroyer : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float delayTime = 5f; // 삭제 대기 시간
    [SerializeField] private string targetTag = "Item"; // 감지할 오브젝트 태그

    private void OnTriggerEnter(Collider other)
    {
        // 1. 특정 태그를 가진 오브젝트인지 확인 (선택 사항이지만 권장)
        if (other.CompareTag(targetTag))
        {
            // 2. 5초 후에 해당 오브젝트를 파괴
            Destroy(other.gameObject, delayTime);

            //Debug.Log($"{other.name}이(가) {delayTime}초 뒤에 삭제됩니다.");
        }
    }
}