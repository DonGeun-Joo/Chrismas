using UnityEngine;

[ExecuteAlways] // 이 줄을 추가하면 플레이 모드가 아닐 때도 코드가 작동합니다!
public class ExpandBounds : MonoBehaviour
{
    void Awake() // 혹은 Update에서 한 번 실행
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 5f);
        }
    }
}