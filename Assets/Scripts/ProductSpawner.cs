using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 리스트 사용을 위해 추가

public class ProductSpawner : MonoBehaviour
{
    [Header("생성할 제품 목록")]
    // 배열을 사용하여 여러 개의 프리팹을 담을 수 있게 합니다.
    public GameObject[] productPrefabs;

    [Header("설정")]
    public Transform spawnPoint;    // 제품이 생성될 정확한 위치
    public float spawnInterval = 5f; // 생성 주기 (5초)

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnRandomProduct();
        }
    }

    void SpawnRandomProduct()
    {
        // 프리팹 목록이 비어있는지 확인
        if (productPrefabs == null || productPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("Spawner: 프리팹 목록이 비었거나 SpawnPoint가 할당되지 않았습니다!");
            return;
        }

        // 0부터 리스트의 개수(Length) 사이에서 랜덤 선택
        int randomIndex = Random.Range(0, productPrefabs.Length);
        GameObject selectedPrefab = productPrefabs[randomIndex];

        // 선택된 제품 생성
        if (selectedPrefab != null)
        {
            Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }
}