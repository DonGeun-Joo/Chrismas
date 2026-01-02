using System;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class OpticalSensor : MonoBehaviour
{
    [Header("구조 설정")]
    public Transform firePoint;
    public Transform recievePoint;
    public Transform receiver;
    public LineRenderer lineRenderer;
    public float maxDistance = 0.032f;

    [Header("감지 대상 설정")]
    public GameObject targetItem;

    [Header("이벤트")]
    public UnityEvent OnDetected;
    public UnityEvent OnLost;

    public bool showLog = false;
    private bool isDetected = false; // 현재 감지 상태 저장

    void Start()
    {
        if (lineRenderer != null)
        {
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
        Vector3 direction = (receiver != null) ? (recievePoint.position - firePoint.position).normalized : -firePoint.right;
        float dist = (receiver != null) ? Vector3.Distance(firePoint.position, recievePoint.position) : maxDistance;

        RaycastHit hit;
        bool hitAnything = Physics.Raycast(firePoint.position, direction, out hit, dist);

        // 시각화 로직
        lineRenderer.SetPosition(0, firePoint.position);

        bool currentHitState = false;

        if (hitAnything)
        {
            lineRenderer.SetPosition(1, hit.point);

            if (receiver != null)
            {
                // 수광부 방식: 수광부가 아닌 물체에 가려졌을 때가 "감지"
                if (hit.transform != receiver)
                {
                    currentHitState = CheckIsTarget(hit.transform.gameObject);
                }
            }
            else
            {
                // 단독 센서 방식: 물체에 닿았을 때가 "감지"
                currentHitState = CheckIsTarget(hit.transform.gameObject);
            }
        }
        else
        {
            Vector3 endPos = (receiver != null) ? recievePoint.position : firePoint.position + (direction * maxDistance);
            lineRenderer.SetPosition(1, endPos);
        }

        // --- 상태 변화 감지 및 이벤트 호출 (핵심) ---
        if (currentHitState && !isDetected)
        {
            isDetected = true;
            OnDetected.Invoke();
            if (showLog) Debug.Log($"{gameObject.name}: Detected");
        }
        else if (!currentHitState && isDetected)
        {
            isDetected = false;
            OnLost.Invoke();
            if (showLog) Debug.Log($"{gameObject.name}: Lost");
        }
    }

    bool CheckIsTarget(GameObject hitObject)
    {
        if (targetItem == null) return true; // 타겟 설정 안되어 있으면 모든 물체 감지
        return hitObject.name.StartsWith(targetItem.name);
    }

    internal bool GetDetectedState()
    {
        return isDetected;
    }
}