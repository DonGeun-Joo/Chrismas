using UnityEngine;
using UnityEngine.Events;

public class AutoSwitch : MonoBehaviour
{
    [Header("감지 대상 설정")]
    public GameObject targetRod;

    [Header("시각화 설정 (Self Emission)")]
    [ColorUsage(true, true)]
    public Color activeEmissionColor = Color.green;

    [Header("이벤트")]
    public UnityEvent OnDetected;
    public UnityEvent OnLost;

    private Material _material;

    void Start()
    {
        // 자기 자신의 렌더러에서 머티리얼을 가져옵니다.
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            _material = renderer.material;
            SetEmission(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetRod == null) return;

        if (other.gameObject == targetRod || other.transform.IsChildOf(targetRod.transform))
        {
            SetEmission(true);
            OnDetected.Invoke(); // InputAdapter가 이 신호를 낚아챕니다.
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetRod == null) return;

        if (other.gameObject == targetRod || other.transform.IsChildOf(targetRod.transform))
        {
            SetEmission(false);
            OnLost.Invoke(); // InputAdapter가 이 신호를 낚아챕니다.
        }
    }

    private void SetEmission(bool isOn)
    {
        if (_material == null) return;

        if (isOn)
            _material.EnableKeyword("_EMISSION");
        else
            _material.DisableKeyword("_EMISSION");
    }
}