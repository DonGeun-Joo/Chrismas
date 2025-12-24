using UnityEngine;
using System;

public class AjinManager : MonoBehaviour
{
    // 어느 스크립트에서든 AjinManager.Instance로 접근 가능하게 설정
    public static AjinManager Instance { get; private set; }

    void Awake()
    {
        // 1. 싱글톤 구성: 중복 생성 방지 및 씬 전환 시 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 2. 아진 라이브러리 초기화 호출
            InitializeAjin();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAjin()
    {
        // AxlOpen(7): 라이브러리를 사용하기 위해 메모리에 올리는 필수 함수
        // 7은 보통 기본 오픈 모드를 의미합니다.
        uint retCode = CAXL.AxlOpen(7);

        if (retCode == 0) // 0 == AXT_RT_SUCCESS
        {
            Debug.Log("<color=green>아진 라이브러리 초기화 성공!</color>");

            // 실물 보드 개수 확인 (앞서 대화한 가상 모드 체크 로직)
            int boardCount = 0;
            CAXL.AxlGetBoardCount(ref boardCount);

            if (boardCount == 0)
                Debug.LogWarning("감지된 실물 보드가 없습니다. 가상 모드로 동작합니다.");
            else
                Debug.Log($"{boardCount}개의 아진 보드가 감지되었습니다.");
        }
        else
        {
            Debug.LogError($"아진 라이브러리 초기화 실패! 에러 코드: {retCode}");
            // 에러 발생 시 사용자에게 알림을 띄우거나 앱을 종료하는 처리가 필요할 수 있습니다.
        }
    }

    void OnApplicationQuit()
    {
        // 3. 프로그램 종료 시 반드시 호출하여 메모리 해제
        CAXL.AxlClose();
        Debug.Log("아진 라이브러리가 안전하게 종료되었습니다.");
    }
}