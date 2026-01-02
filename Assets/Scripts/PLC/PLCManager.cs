using UnityEngine;
using System.Collections;
using ActUtlType64Lib;
using System.Threading;

public class PLCManager : MonoBehaviour
{
    public static PLCManager Instance;
    private ActUtlType64 plc;

    // 통신 데이터를 담을 배열 (여러 스레드에서 접근하므로 volatile 사용 고려)
    public int[] yStatuses = new int[10];

    private Thread plcThread;
    private bool isRunning = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. 통신용 별도 스레드 생성
        isRunning = true;
        plcThread = new Thread(RunPlcCommunication);
        plcThread.Start();
    }

    // [중요] 이 함수는 메인 스레드가 아닌 '별도 스레드'에서 무한 반복됩니다.
    void RunPlcCommunication()
    {
        // COM 객체는 사용하는 스레드 안에서 생성하는 것이 가장 안전합니다.
        plc = new ActUtlType64();
        plc.ActLogicalStationNumber = 1;

        int result = plc.Open();
        if (result != 0) return;

        while (isRunning)
        {
            // 통신 실행 (여기서 발생하는 지연은 유니티 화면에 영향을 주지 않음)
            plc.ReadDeviceBlock("Y20", 10, out yStatuses[0]);

            // CPU 점유율 과다 방지를 위해 아주 짧은 휴식 (10ms)
            Thread.Sleep(10);
        }

        plc.Close();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (plcThread != null && plcThread.IsAlive)
        {
            plcThread.Join(); // 스레드가 안전하게 종료될 때까지 대기
        }
    }
}