using UnityEngine;
using EasyModbus;
using System;

public class UR_ModbusBridge : MonoBehaviour
{
    [Header("urSim Connection Settings")]
    public string urIpAddress = "192.168.56.101";
    public int port = 502;

    private ModbusClient _modbusClient;

    [Header("urSim IO Mapping")]
    public ushort urDO0_Address = 16;   // 로봇의 Digital Output 0번
    public ushort working_CV = 128; // 로봇의 GP Register (D128 수신용)
    public ushort canMoving = 129;

    // 로봇 상태 변경 시 PLC_InputAdapter가 감지할 수 있도록 이벤트 선언
    public event Action<bool> OnRobotBusyChanged;
    private bool _lastBusyState = false;

    void Start()
    {
        _modbusClient = new ModbusClient(urIpAddress, port);
        // 연결 시도 루틴 시작
        InvokeRepeating(nameof(CheckConnection), 1f, 2f);
        // 데이터 중계 루프 시작 (0.1초 간격)
        InvokeRepeating(nameof(BridgeDataLoop), 2f, 0.1f);
    }

    void CheckConnection()
    {
        if (!_modbusClient.Connected)
        {
            try
            {
                _modbusClient.Connect();
                Debug.Log("<color=green>urSim Modbus Connected!</color>");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"urSim Reconnecting... : {e.Message}");
            }
        }
    }

    void BridgeDataLoop()
    {
        if (!_modbusClient.Connected) return;

        try
        {
            // --- 1. urSim DO 0번 읽기 (Robot Busy 상태) ---
            bool[] urCoils = _modbusClient.ReadCoils(urDO0_Address, 1);
            bool currentBusy = urCoils[0];

            // 상태가 변했을 때만 이벤트를 발생시켜 PLC 통신 부하 감소
            if (currentBusy != _lastBusyState)
            {
                _lastBusyState = currentBusy;
                OnRobotBusyChanged?.Invoke(currentBusy);
                //Debug.Log($"[urSim] Robot Busy State Changed: {currentBusy}");
            }

            // --- 2. PLC -> urSim 데이터 전달 (D128 데이터 읽어서 urSim에 쓰기) ---
            if (IO_Manager.Instance != null)
            {
                short plcValue = IO_Manager.Instance.GetRegister("D128");
                _modbusClient.WriteSingleRegister(working_CV, plcValue);

                plcValue = IO_Manager.Instance.GetRegister("D129");
                _modbusClient.WriteSingleRegister(canMoving, plcValue);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"UR Modbus Bridge Error: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (_modbusClient != null && _modbusClient.Connected)
        {
            _modbusClient.Disconnect();
        }
    }
}