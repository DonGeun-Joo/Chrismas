using UnityEngine;
using System;

public class AjinextekManager : MonoBehaviour
{
    // 어디서든 "AjinextekManager.Instance"로 접근할 수 있게 함 (싱글톤)
    public static AjinextekManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음

            InitLibrary(); // 시작하자마자 라이브러리 오픈!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitLibrary()
    {
        // 1. 라이브러리 오픈 (7: 기본 모드)
        uint ret = CAXL.AxlOpen(7);

        if (ret == 0) // 0이면 성공
        {
            Debug.Log("<color=cyan>아진엑스텍 라이브러리가 성공적으로 열렸습니다.</color>");

            // 2. 보드 개수 확인
            int boardCount = 0;
            CAXL.AxlGetBoardCount(ref boardCount);

            if (boardCount == 0)
            {
                Debug.LogWarning("실물 보드가 없습니다. '가상 모드'로 동작을 시작합니다.");
            }
            else
            {
                Debug.Log($"{boardCount}개의 보드가 감지되었습니다. 실물 제어 모드입니다.");
            }
        }
        else
        {
            Debug.LogError($"라이브러리 오픈 실패! 에러 코드: {ret}");
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            // 라이브러리가 정상적으로 열려 있을 때만 Close 호출
            CAXL.AxlClose();
            Debug.Log("아진엑스텍 라이브러리를 안전하게 닫았습니다.");
        }
        catch (DllNotFoundException)
        {
            Debug.LogWarning("DLL을 찾을 수 없어 종료 처리를 건너뜁니다.");
        }
    }
}