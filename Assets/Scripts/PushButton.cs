using UnityEngine;
using UnityEngine.Events;

public class Push_Button : MonoBehaviour
{
    [Header("버튼 가동부 설정")]
    public Transform buttonMesh;
    public float pressDistance = 0.01f;
    public float pressSpeed = 20f; // 즉시 반응을 위해 속도를 조금 높였습니다.

    [Header("이벤트")]
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

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            _material = renderer.material;
            SetEmission(false);
        }
    }

    void Update()
    {
        if (buttonMesh != null)
        {
            buttonMesh.localPosition = Vector3.Lerp(buttonMesh.localPosition, _targetPos, Time.deltaTime * pressSpeed);
        }
    }

    // [핵심] 조준이 맞고 클릭하는 순간 즉시 호출
    public void StartInteraction()
    {
        if (_isPressed) return;

        _isPressed = true;
        _targetPos = _startPos + (Vector3.back * pressDistance);

        OnPressed.Invoke();
    }

    // [핵심] 조준이 벗어나거나 마우스를 떼는 순간 즉시 호출
    public void StopInteraction()
    {
        if (!_isPressed) return;

        _isPressed = false;
        _targetPos = _startPos;

        OnReleased.Invoke();
    }

    public void SetEmission(bool isOn)
    {
        //Debug.Log("Emission");
        if (_material == null) return;
        if (isOn) _material.EnableKeyword("_EMISSION");
        else _material.DisableKeyword("_EMISSION");
    }
}