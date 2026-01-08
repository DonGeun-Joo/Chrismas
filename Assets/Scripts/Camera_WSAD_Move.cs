using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_WSAD_Move : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float boostMultiplier = 2f;
    private Vector2 _moveInput;
    private bool _isBoosted;


    // Input System 메시지 수신 (Player Input 컴포넌트의 Send Messages 방식)
    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    private CinemachineCamera _vcam;
    private CinemachineInputAxisController _inputController;

    [SerializeField] private bool isMoveActive = true; // 이동/회전 가능 여부

    void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();
        // 시네머신의 마우스 입력을 관리하는 컴포넌트를 가져옵니다.
        _inputController = GetComponent<CinemachineInputAxisController>();
    }

    void Update()
    {
        // 불값이 true일 때만 이동 로직 실행
        if (isMoveActive)
        {
            ApplyMovement();
        }
    }

    // Input System에서 호출 (토글 방식: 누를 때마다 상태 반전)
    public void OnStop(InputValue value)
    {
            Debug.Log("OnStop");
        if (value.isPressed)
        {
            Debug.Log("In If");
            isMoveActive = !isMoveActive;

            // 핵심: 시네머신 회전 입력 컴포넌트를 켜고 끕니다.
            if (_inputController != null)
            {
                _inputController.enabled = isMoveActive;
            }

            // 마우스 커서 상태 제어 (선택 사항)
            Cursor.lockState = isMoveActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isMoveActive;

            Debug.Log($"카메라 조작 상태: {isMoveActive}");
        }
    }

    private void ApplyMovement()
    {
        // 입력이 없으면 계산하지 않음
        if (_moveInput == Vector2.zero) return;

        float currentSpeed = _isBoosted ? moveSpeed * boostMultiplier : moveSpeed;

        // 핵심: 현재 카메라(Cinemachine Camera)의 전면과 오른쪽 방향을 가져옴
        // transform.forward는 카메라가 현재 보고 있는 정확한 방향(모든 축 회전 반영)임
        Vector3 lookDir = transform.forward;
        Vector3 rightDir = transform.right;

        // W/S(y축 입력)는 바라보는 방향으로, A/D(x축 입력)는 바라보는 방향의 옆으로 계산
        Vector3 desiredMove = (lookDir * _moveInput.y) + (rightDir * _moveInput.x);

        // 실제 위치 이동 (바라보는 방향으로 즉시 투영)
        transform.position += desiredMove * currentSpeed * Time.deltaTime;
    }
}
