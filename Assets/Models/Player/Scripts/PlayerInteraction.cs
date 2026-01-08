using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3.0f;
    public LayerMask interactLayer;

    private Transform _camTransform;
    private bool _isHoldingInteract = false;
    private Push_Button _currentButton;
    private PlayerMove _playerMove; // PlayerMove 참조용

    void Start()
    {
        _camTransform = Camera.main.transform;
        _playerMove = GetComponent<PlayerMove>(); // PlayerMove 컴포넌트 가져오기
    }

    // ⭐ 수정 포인트 1: 즉시 실행 및 상태 업데이트
    private void OnInteract(InputValue value)
    {
        _isHoldingInteract = value.isPressed;

        if (_isHoldingInteract)
        {
            // 클릭한 순간 즉시 버튼을 찾아 누릅니다. (Update를 기다리지 않음)
            CheckButtonUnderCrosshair();
        }
        else
        {
            // 마우스를 떼는 순간 즉시 버튼을 해제합니다.
            ReleaseCurrentButton();
        }
    }

    void Update()
    {
        // ⭐ 수정 포인트 2: 마우스를 누르고 있는 동안에만 '조준점 이탈' 여부를 감시합니다.
        if (_isHoldingInteract)
        {
            CheckButtonUnderCrosshair();
        }
    }

    void CheckButtonUnderCrosshair()
    {
        Ray ray;

        // ⭐ 수정 포인트: 커서 모드에 따라 레이 생성 방식 변경
        if (_playerMove != null && _playerMove.IsCursorMode)
        {
            // 마우스 위치에서 레이를 발사 (화면 클릭 대응)
            ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        }
        else
        {
            // 화면 중앙(크로스헤어)에서 레이를 발사
            ray = new Ray(_camTransform.position, _camTransform.forward);
        }
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            Push_Button newButton = hit.collider.GetComponent<Push_Button>();

            if (newButton != null)
            {
                // 다른 버튼을 조준했다면 이전 버튼은 해제
                if (_currentButton != null && _currentButton != newButton)
                {
                    _currentButton.StopInteraction();
                }

                if (_currentButton != newButton)
                {
                    _currentButton = newButton;
                    _currentButton.StartInteraction(); // 즉시 Press 호출
                }
            }
            else
            {
                // 버튼이 아닌 물체를 조준하면 해제
                ReleaseCurrentButton();
            }
        }
        else
        {
            // 허공을 조준하면 해제
            ReleaseCurrentButton();
        }
    }

    void ReleaseCurrentButton()
    {
        if (_currentButton != null)
        {
            _currentButton.StopInteraction(); // 즉시 Release 호출
            _currentButton = null;
        }
    }
}