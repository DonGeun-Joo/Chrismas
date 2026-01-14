using UnityEngine;
using EasyModbus;
using System;
using System.Threading;
using System.Threading.Tasks;

public class UR_ModbusBridge : MonoBehaviour
{
    [Header("urSim Connection Settings")]
    public string urIpAddress = "192.168.56.101";
    public int port = 502;

    private ModbusClient _modbusClient;
    private CancellationTokenSource _cts; // 비동기 작업 종료용

    [Header("urSim IO Mapping")]
    public ushort urDO0_Address = 16;
    public ushort working_CV = 128;
    public ushort canMoving = 129;

    public MotorControlPanel MotorControlPanel;

    public event Action<bool> OnRobotBusyChanged;



    // 스레드 간 공유 변수
    private bool _currentBusy = false;
    private bool _grip = false;
    private bool _lastBusyState = false;
    private short _plcD128 = 0;
    private short _plcD129 = 0;
    private bool _isConnected = false;

    void Start()
    {
        _modbusClient = new ModbusClient(urIpAddress, port);
        _modbusClient.ConnectionTimeout = 1000; // 타임아웃 1초로 제한

        _cts = new CancellationTokenSource();

        // 별도 스레드에서 통신 루프 시작
        Task.Run(() => ModbusCommunicationLoop(_cts.Token));
    }

    void Update()
    {
        // 1. PLC 데이터를 메인 스레드에서 미리 읽어둠 (IO_Manager는 메인 스레드 전용일 가능성 높음)
        if (IO_Manager.Instance != null)
        {
            _plcD128 = IO_Manager.Instance.GetRegister("D128");
            _plcD129 = IO_Manager.Instance.GetRegister("D129");
        }

        // 2. 통신 스레드에서 가져온 Busy 상태 변화 감지 및 이벤트 발생
        if (_currentBusy != _lastBusyState)
        {
            _lastBusyState = _currentBusy;
            OnRobotBusyChanged?.Invoke(_currentBusy);
        }
    }

    /// <summary>
    /// 별도 스레드에서 실행되는 통신 루프
    /// </summary>
    private async Task ModbusCommunicationLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!_modbusClient.Connected)
                {
                    _isConnected = false;
                    _modbusClient.Connect();
                    _isConnected = true;
                    Debug.Log("<color=green>[urSim] Connected!</color>");
                }

                if (_modbusClient.Connected)
                {
                    // --- 1. 데이터 읽기 ---
                    bool[] urCoils = _modbusClient.ReadCoils(urDO0_Address, 2);
                    _currentBusy = urCoils[0];
                    _grip = urCoils[1];

                    // --- 2. 데이터 쓰기 ---
                    _modbusClient.WriteSingleRegister(working_CV, _plcD128);
                    _modbusClient.WriteSingleRegister(canMoving, MotorControlPanel.GetMotionStatus());
                }
            }
            catch (Exception)
            {
                _isConnected = false;
                // 접속 실패 시 로그는 너무 자주 찍히지 않도록 조절하거나 생략
            }

            // 통신 주기 조절 (0.1초 대기) - Task.Delay는 스레드를 차단하지 않음
            await Task.Delay(100, token);
        }
    }

    void OnDestroy()
    {
        // 스레드 종료 및 연결 해제
        _cts?.Cancel();
        if (_modbusClient != null && _modbusClient.Connected)
        {
            _modbusClient.Disconnect();
        }
    }
}