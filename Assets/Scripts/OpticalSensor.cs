using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class OpticalSensor : MonoBehaviour
{
    [Header("구조 설정")]
    public Transform firePoint;      // 발광부 위치
    public Transform recievePoint;      // 발광부 위치
    public Transform receiver;       // 수광부 (없으면 null 가능)
    public LineRenderer lineRenderer;
    public float maxDistance = 0.032f;   // 수광부 없을 때 최대 사거리

    [Header("부모 컨베이어")]
    public GameObject conveyor;

    [Header("감지 대상 설정")]
    public GameObject targetItem;    // 감지하고 싶은 아이템 (Barrel 혹은 Can 드래그)

    [Header("이벤트")]
    public UnityEvent OnDetected;    // 감지 시 실행
    public UnityEvent OnLost;        // 감지 해제 시 실행 (이 줄 추가)

    protected bool isDetected = false;

    void Start()
    {
        if (lineRenderer != null)
        {
            // 라인이 로컬 좌표가 아닌 실제 월드 좌표를 따르도록 설정
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
        }
    }
    void Update()
    {
        if (firePoint == null || lineRenderer == null) return;
        UpdateSensor();
    }

    void UpdateSensor()
    {
        // 1. 레이 방향 및 거리 계산
        Vector3 direction = (receiver != null) ? (recievePoint.position - firePoint.position).normalized : -firePoint.right;
        float dist = (receiver != null) ? Vector3.Distance(firePoint.position, recievePoint.position) : maxDistance;

        RaycastHit hit;
        bool hitAnything = Physics.Raycast(firePoint.position, direction, out hit, dist);

        // 2. 라인 렌더러 시각화
        lineRenderer.SetPosition(0, firePoint.position);
        if (hitAnything)
        {
            lineRenderer.SetPosition(1, hit.point);

            // 3. 감지 로직 (수광부 여부에 따른 처리)
            if (receiver != null)
            {
                // 수광부 방식: 수광부가 아닌 다른 물체에 가려졌을 때
                if (hit.transform != receiver)
                {
                    CheckIfTarget(hit.transform.gameObject);
                }
                else
                {
                    isDetected = false; // 수광부에 잘 닿고 있음
                }
            }
            else
            {
                // 단독 센서 방식: 앞에 무언가 닿았을 때
                CheckIfTarget(hit.transform.gameObject);
            }
        }
        else
        {
            lineRenderer.SetPosition(1, (receiver != null) ? recievePoint.position : firePoint.position + direction * maxDistance);
            isDetected = false;
        }

        lineRenderer.SetPosition(0, firePoint.position); // 시작점: 발광부

        if (hitAnything)
        {
            lineRenderer.SetPosition(1, hit.point); // 끝점: 부딪힌 곳
        }
        else
        {
            // 아무것도 안 부딪혔을 때: 수광부 위치 또는 최대 사거리까지 그리기
            Vector3 endPos = (receiver != null) ? recievePoint.position : firePoint.position + (direction * maxDistance);
            lineRenderer.SetPosition(1, endPos);
        }
    }
    public bool GetDetectedState()
    {
        return isDetected;
    }

    void CheckIfTarget(GameObject hitObject)
    {
        if (targetItem == null) return;

        // 생성된 클론(Clone)도 인식할 수 있도록 이름의 앞부분이 일치하는지 확인
        if (hitObject.name.StartsWith(targetItem.name))
        {
            if (!isDetected)
            {
                isDetected = true;
                OnDetected.Invoke();
                //Debug.Log($"{conveyor.name}: {targetItem.name} 감지됨!");
            }
        }
        else
        {
            isDetected = false;
            OnLost.Invoke(); // 이 줄 추가
        }
    }
}