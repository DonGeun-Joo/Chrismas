using UnityEngine;
using System.Collections;

public class ProductSpawner : MonoBehaviour
{
    [Header("생성할 제품 목록")]
    public GameObject[] productPrefabs;

    [Header("설정")]
    public Transform spawnPoint;
    public float spawnInterval = 1f;

    [Header("애니메이터 설정")]
    public Animator handleAnimator; // Handle 오브젝트의 Animator 연결
    public Animator velveAnimator;  // Velve 오브젝트의 Animator 연결

    private bool _machineOn;

    void Start()
    {
        // 만약 인스펙터에서 연결하지 않았다면 자식에서 자동으로 찾아봅니다.
        if (handleAnimator == null && transform.Find("Handle") != null)
            handleAnimator = transform.Find("Handle").GetComponent<Animator>();

        if (velveAnimator == null && transform.Find("Velve") != null)
            velveAnimator = transform.Find("Velve").GetComponent<Animator>();

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (_machineOn)
            {
                SpawnRandomProduct();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnRandomProduct()
    {
        if (productPrefabs == null || productPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("Spawner: 설정이 누락되었습니다!");
            return;
        }

        int randomIndex = Random.Range(0, productPrefabs.Length);
        GameObject selectedPrefab = productPrefabs[randomIndex];

        if (selectedPrefab != null)
        {
            Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation);

            // [추가] 제품 생성 시 Handle의 Spawn 트리거 실행
            if (handleAnimator != null)
            {
                handleAnimator.SetTrigger("Spawn");
            }
        }
    }

    public void SetMachine(bool machineOn)
    {
        _machineOn = machineOn;

        // [추가] 머신 상태에 따라 Velve의 Machine On 파라미터 제어
        if (velveAnimator != null)
        {
            velveAnimator.SetBool("Machine On", machineOn);
        }
    }
}