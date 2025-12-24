using UnityEngine;
using System.Collections;

public class HomeReturnButton : MonoBehaviour
{
    [Header("Axis Settings")]
    public int axisNo = 0;

    [Header("Homing Speeds")]
    public double velFirst = 5000;  // 1차 탐색 속도 (빠르게)
    public double velSecond = 1000; // 2차 탐색 속도 (느리게)
    public double velLast = 100;    // 최종 정밀 탐색 속도
    public double accel = 10000;    // 가속도

    [Header("Homing Method")]
    [Tooltip("0: -방향, 1: +방향")]
    public int homeDir = 0;         // 보통 마이너스 방향으로 홈을 찾습니다.

    [Tooltip("4: HomeSensor (추천), 0: PosLimit, 1: NegLimit")]
    public uint homeSignal = 4;     // 유니티 모델에 있는 Home 센서를 사용

    public void StartHomeReturn()
    {
        if (AjinextekManager.Instance == null) return;

        // 1. 홈 복귀 파라미터 설정
        // AxmHomeSetMethod(축번호, 방향, 사용할 신호, Z상사용여부, 클리어타임, 오프셋)
        CAXM.AxmHomeSetMethod(axisNo, homeDir, homeSignal, 0, 100, 0);

        // 2. 홈 복귀 속도 설정
        // AxmHomeSetVel(축번호, 1차속도, 2차속도, 3차속도, 마지막속도, 1차가속, 2차가속)
        CAXM.AxmHomeSetVel(axisNo, velFirst, velSecond, velSecond, velLast, accel, accel);

        // 3. (중요) 홈 완료 후 좌표를 0으로 자동 초기화 설정
        // AxmHomeSetFineAdjust(축번호, 독길이, 스캔타임, 정밀사용, 클리어사용(1))
        CAXM.AxmHomeSetFineAdjust(axisNo, 0, 100, 1, 1);

        // 4. 홈 복귀 시작
        uint ret = CAXM.AxmHomeSetStart(axisNo);

        if (ret == 0)
        {
            Debug.Log($"축 {axisNo}: 홈 복귀 시퀀스 시작!");
            StartCoroutine(CheckHomeResult());
        }
        else
        {
            Debug.LogError($"홈 복귀 시작 실패 에러 코드: {ret}");
        }
    }

    // 홈 복귀가 끝났는지 감시하는 코루틴
    IEnumerator CheckHomeResult()
    {
        uint result = 0;
        while (true)
        {
            // 홈 복귀 결과 확인
            CAXM.AxmHomeGetResult(axisNo, ref result);

            if (result == 1) // HOME_SUCCESS (완료)
            {
                Debug.Log("<color=yellow>홈 복귀 성공! 좌표가 0으로 초기화되었습니다.</color>");
                yield break;
            }
            else if (result > 2 && result != 0xFF) // 에러 발생 시 (HOME_SEARCHING=2 제외)
            {
                Debug.LogError($"홈 복귀 중 에러 발생! 결과 코드: {result}");
                yield break;
            }

            yield return new WaitForSeconds(0.2f); // 0.2초마다 체크
        }
    }
}