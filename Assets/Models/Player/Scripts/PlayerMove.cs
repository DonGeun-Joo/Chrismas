using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 1.0f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Animator animator;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Transform mainCamTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        mainCamTransform = Camera.main.transform;

        // 마우스 커서 숨기기 및 중앙 고정
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        Move();
        UpdateAnimation();
    }

    void Move()
    {
        // [핵심 추가] 캐릭터의 회전을 카메라의 좌우 회전값과 일치시킵니다.
        // mainCamTransform.eulerAngles.y는 카메라가 바라보는 수평 방향 각도입니다.
        transform.rotation = Quaternion.Euler(0, mainCamTransform.eulerAngles.y, 0);

        // 1. 카메라의 방향을 가져오되, 위아래 성분(Y)은 무시합니다
        Vector3 forward = mainCamTransform.forward;
        Vector3 right = mainCamTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // 2. 실제 이동 방향 계산
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // 3. 캐릭터 컨트롤러로 이동 실행
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 4. 중력 처리 (땅에 붙어있게 함)
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimation()
    {
        if (animator != null)
        {
            float speed = moveInput.magnitude;
            animator.SetFloat("Speed", speed);
        }
    }
}