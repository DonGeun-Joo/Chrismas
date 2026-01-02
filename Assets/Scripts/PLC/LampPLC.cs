using UnityEngine;

public class LampPLC : MonoBehaviour
{
    [Header("PLC 설정")]
    public string lampAddress; // PLC의 출력 주소 (예: Green=Y0, Yellow=Y1, Red=Y2)

    [Header("렌더러 설정")]
    public MeshRenderer lampRenderer;

    private Material _lampMaterial;

    void Start()
    {
        if (lampRenderer == null) lampRenderer = GetComponent<MeshRenderer>();
        _lampMaterial = lampRenderer.material;

        // PLC 주소 감시 등록
        MXRequester.Get.AddDeviceAddress(lampAddress, UpdateLampStatus);
    }

    private void UpdateLampStatus(short value)
    {
        if (value == 1) // 램프 ON
        {
            _lampMaterial.EnableKeyword("_EMISSION");
        }
        else // 램프 OFF
        {
            _lampMaterial.DisableKeyword("_EMISSION");
        }
    }
}