using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Push_Button : MonoBehaviour
{
    [Header("버튼 가동부 설정")]
    [Tooltip("실제로 아래로 눌릴 버튼 메쉬를 드래그하세요.")]
    public Transform buttonMesh;
    public float pressDistance = 0.005f; // 눌리는 깊이
    public float pressSpeed = 10f;       // 눌리는 속도

    [Header("이벤트 (InputAdapter 연동)")]
    public UnityEvent OnPressed;
    public UnityEvent OnReleased;

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _isPressed = false;

    private Material _material;


    void Start()
    {
        if (buttonMesh != null)
        {
            _startPos = buttonMesh.localPosition;
            _targetPos = _startPos;
        }

        // 자기 자신의 렌더러에서 머티리얼을 가져옵니다.
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            _material = renderer.material;
            SetEmission(false);
        }
    }

    void Update()
    {
        // 버튼의 부드러운 움직임 처리 (시각적 효과)
        if (buttonMesh != null)
        {
            buttonMesh.localPosition = Vector3.Lerp(buttonMesh.localPosition, _targetPos, Time.deltaTime * pressSpeed);
        }
    }

    // 마우스로 버튼을 누를 때 (Collider 필요)
    private void OnMouseDown()
    {
        if (_isPressed) return;

        _isPressed = true;
        _targetPos = _startPos + (Vector3.down * pressDistance);
        OnPressed.Invoke(); // PLC_InputAdapter로 '1' 전송
    }

    // 마우스를 뗄 때
    private void OnMouseUp()
    {
        if (!_isPressed) return;

        _isPressed = false;
        _targetPos = _startPos;
        OnReleased.Invoke(); // PLC_InputAdapter로 '0' 전송
    }

    // 외부에서 버튼의 현재 눌림 상태를 확인할 때 사용
    public bool IsPressed => _isPressed;

    public void SetEmission(bool isOn)
    {
        if (_material == null) return;

        if (isOn)
            _material.EnableKeyword("_EMISSION");
        else
            _material.DisableKeyword("_EMISSION");
    }
}