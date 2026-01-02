using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private Vector2 textureDirection = Vector2.up;

    private Rigidbody _rb;
    private Renderer _renderer;
    private Material _material;

    // --- 추가된 부분: 가동 상태 제어 ---
    private bool _isMoving = false;
    private float _currentOffset = 0f; // 텍스처 오프셋 누적값

    // PLC_OutputAdapter에서 호출할 함수
    public void SetRunning(bool state)
    {
        _isMoving = state;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();

        if (_renderer != null)
        {
            _material = _renderer.material;
        }
    }

    private void FixedUpdate()
    {
        // 가동 중일 때만 텍스처를 흐르게 함
        if (_isMoving)
        {
            ScrollTexture();
        }
    }

    private void ScrollTexture()
    {
        if (_material != null)
        {
            // Time.time을 직접 곱하면 멈췄다 가동할 때 텍스처가 튑니다.
            // 따라서 델타 타임을 활용해 오프셋을 누적시킵니다.
            _currentOffset += speed * Time.fixedDeltaTime * 0.1f;
            _material.mainTextureOffset = textureDirection * _currentOffset;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 가동 중이 아닐 때는 물체를 밀지 않음
        if (!_isMoving) return;

        Rigidbody targetRb = collision.rigidbody;

        if (targetRb != null && !targetRb.isKinematic)
        {
            Vector3 worldDirection = transform.TransformDirection(moveDirection).normalized;
            Vector3 newPosition = targetRb.position + (worldDirection * speed * Time.fixedDeltaTime);
            targetRb.MovePosition(newPosition);
        }
    }
}