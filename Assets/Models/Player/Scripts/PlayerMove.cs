using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 1.0f;
    public float gravity = -9.81f;

    [Header("Cinemachine 3.x 설정")]
    public CinemachineInputAxisController inputController;

    private CharacterController controller;
    private Animator animator;
    private Vector2 moveInput; // ⭐ 이 값을 업데이트하는 OnMove가 필요합니다.
    private Vector3 velocity;
    private Transform mainCamTransform;
    private bool isCursorMode = false;
    public bool IsCursorMode => isCursorMode;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamTransform = Camera.main.transform;

        if (inputController == null)
            inputController = GameObject.FindAnyObjectByType<CinemachineInputAxisController>();

        UpdateCursorState();
    }

    // ⭐ [추가] 키보드 WASD 입력을 받는 메서드
    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnToggleCursor(InputValue value)
    {
        if (value.isPressed)
        {
            isCursorMode = !isCursorMode;
            UpdateCursorState();
        }
    }

    private void UpdateCursorState()
    {
        if (isCursorMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (inputController != null) inputController.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (inputController != null) inputController.enabled = true;
        }
    }

    void Update()
    {
        Move();
        UpdateAnimation();
    }

    void Move()
    {
        // 커서 모드가 아닐 때만 카메라 방향으로 캐릭터 회전
        if (!isCursorMode)
        {
            transform.rotation = Quaternion.Euler(0, mainCamTransform.eulerAngles.y, 0);
        }

        Vector3 forward = mainCamTransform.forward;
        Vector3 right = mainCamTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // moveInput 값이 0이면 moveDirection도 0이 되어 움직이지 않습니다.
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 중력 처리
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            // moveInput의 크기를 사용하여 애니메이션 속도 조절
            float speed = moveInput.magnitude;
            animator.SetFloat("Speed", speed);
        }
    }
}