using UnityEngine;

public class LampPLC : MonoBehaviour
{

    [Header("렌더러 설정")]
    public MeshRenderer lampRenderer;

    private Material _lampMaterial;

    void Start()
    {
        if (lampRenderer == null) lampRenderer = GetComponent<MeshRenderer>();
        _lampMaterial = lampRenderer.material;
        _lampMaterial.DisableKeyword("_EMISSION");
    }

    public void SetEmission(bool isOn)
    {
        if (isOn) // 램프 ON
        {
            _lampMaterial.EnableKeyword("_EMISSION");
        }
        else // 램프 OFF
        {
            _lampMaterial.DisableKeyword("_EMISSION");
        }
    }
}