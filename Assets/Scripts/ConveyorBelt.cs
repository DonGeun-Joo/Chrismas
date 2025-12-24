using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("컨베이어 벨트의 이동 속도입니다.")]
    [SerializeField] private float speed = 2.0f;

    [Tooltip("물체가 이동할 방향입니다. (로컬 기준)")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    [Tooltip("텍스처가 흐를 방향입니다. (UV 기준)")]
    [SerializeField] private Vector2 textureDirection = Vector2.up;

    private Rigidbody _rb;
    private Renderer _renderer;
    private Material _material;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();

        // 런타임에 머티리얼 인스턴스를 생성하여 원본 훼손 방지
        if (_renderer != null)
        {
            _material = _renderer.material;
        }
    }

    private void FixedUpdate()
    {
        // 1. 시각적 처리 (텍스처 스크롤링)
        // FixedUpdate 대신 Update에서 해도 되지만, 물리 속도와 싱크를 맞추기 위해 여기서 처리해도 무방함
        ScrollTexture();

        // 2. 물리적 처리 (자신을 뒤로 밀어서 물체를 앞으로 보내는 꼼수 대신, 직접 처리 방식 사용)
        // 아래 OnCollisionStay 대신 여기서 직접 Physics.OverlapBox 등을 쓸 수도 있지만,
        // 충돌 감지는 Unity 이벤트 시스템을 활용하는 것이 효율적입니다.
    }

    private void ScrollTexture()
    {
        if (_material != null)
        {
            // 시간 경과에 따른 오프셋 계산 (Time.time 대신 쉐이더 싱크를 위해 누적값 사용 가능하지만 간단히 구현)
            Vector2 offset = textureDirection * (speed * Time.time * 0.1f); // 0.1f는 텍스처 스케일 보정용
            _material.mainTextureOffset = offset;
        }
    }

    // 물리적 충돌이 유지되는 동안 호출됨 (FixedUpdate 주기에 맞춰 실행됨)
    private void OnCollisionStay(Collision collision)
    {
        // 충돌한 물체에 Rigidbody가 있는지 확인
        Rigidbody targetRb = collision.rigidbody;

        if (targetRb != null && !targetRb.isKinematic)
        {
            // 월드 좌표계 기준으로 이동 방향 계산
            // transform.TransformDirection을 사용하여 로컬 방향을 월드 방향으로 변환
            Vector3 worldDirection = transform.TransformDirection(moveDirection).normalized;

            // Rigidbody.MovePosition을 사용하여 물리 엔진을 존중하며 이동
            // 현재 위치 + (방향 * 속도 * 고정 델타 타임)
            Vector3 newPosition = targetRb.position + (worldDirection * speed * Time.fixedDeltaTime);

            targetRb.MovePosition(newPosition);
        }
    }
}