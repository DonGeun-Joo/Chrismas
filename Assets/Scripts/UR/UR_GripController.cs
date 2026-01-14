using UnityEngine;

public class UR_GripController : MonoBehaviour
{
    private Animator _animator;

    [Header("Settings")]
    public string gripParameterName = "Grip";

    [Header("Tags")]
    public string pickZoneTag = "Pick";    // 잡는 구역 태그
    public string placeZoneTag = "Place";  // 놓는 구역 태그
    public string itemTag = "Item";        // 아이템 태그
    public string boxTag = "Box";          // 최종 목적지 Box 태그

    private bool _isGrip = false;
    private GameObject _grabbedItem;       // 현재 잡고 있는 아이템
    private GameObject _targetBox;         // 현재 닿아있는 박스
    private bool _isPickZone;


    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Pick 오브젝트의 Collider에 닿으면 그리퍼를 닫음
        if (other.CompareTag(pickZoneTag))
        {
            _isPickZone = true;
            SetGrip(true);
            Debug.Log("Pick Zone 진입: 그리퍼 닫힘");
            
        }

        if (other.CompareTag(placeZoneTag))
        {
            _isPickZone = false;
            SetGrip(false);
            ReleaseItem(other.gameObject);
            Debug.Log("Place Zone 진입: 그리퍼 열림");
        }

        if (other.CompareTag(itemTag) && _isPickZone && _grabbedItem is null)
        {
            GrabItem(other.gameObject);
            Debug.Log("아이템을 툴의 자식으로 변경");
        }


    }

    public void SetGrip(bool isGripping)
    {
        _isGrip = isGripping;
        if (_animator != null)
        {
            _animator.SetBool(gripParameterName, isGripping);
        }
    }

    private void GrabItem(GameObject item)
    {
        _grabbedItem = item;
        _grabbedItem.transform.SetParent(transform);

        // 물리 충돌 방지
        Rigidbody rb = _grabbedItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Debug.Log($"{item.name}을(를) Tool의 자식으로 등록");
    }

    private void ReleaseItem(GameObject zone)
    {
        if (_grabbedItem != null)
        {
            // 1. 내려놓을 대상(Box) 설정
            // zone은 PlaceZone이므로, 실제 박스 오브젝트를 찾거나 zone 자체를 부모로 설정합니다.
            _targetBox = zone;

            // 박스가 없으면 월드 공간으로 독립시킴
            _grabbedItem.transform.SetParent(null);
              

            // 2. 물리 엔진 다시 활성화 (중력 영향 등을 받게 함)
            Rigidbody rb = _grabbedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true; // 중력이 필요하다면 체크
            }

            // 3. 변수 초기화
            _grabbedItem = null;
        }
    }
}