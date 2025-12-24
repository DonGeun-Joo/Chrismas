using UnityEngine;

public class ConveyorController : MonoBehaviour
{
    public Vector2 direction = Vector2.up;
    public float speedPerSec = 1f;

    private Material _material;
    private float _uvOffset;

    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        _uvOffset += speedPerSec * Time.deltaTime;
        if (_uvOffset > 1f)
        {
            _uvOffset -= 1f;
        }

        _material.SetTextureOffset("_BaseMap", direction * _uvOffset);
        _material.SetTextureOffset("_BumpMap", direction * _uvOffset);
    }
}
