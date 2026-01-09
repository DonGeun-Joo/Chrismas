using UnityEngine;
using System.Collections;
using NModbus; // NModbus4 라이브러리 사용 가정
using System.Net.Sockets;

public class UR_ModbusBridge : MonoBehaviour
{
    [Header("urSim Connection Settings")]
    public string urIpAddress = "192.168.56.101"; // 가상머신 IP
    public int port = 502;

    private TcpClient _tcpClient;
    private IModbusMaster _modbusMaster;
    private bool _isConnected = false;

    [Header("Bridge Settings")]
    [Tooltip("urSim 130번(DO)을 PLC의 어떤 비트로 보낼지 설정")]
    public string plcJobDoneBit = "M130";
    [Tooltip("PLC D128을 urSim의 몇 번 주소로 보낼지 설정")]
    public ushort urTargetAddress = 128;

    void Start()
    {
        StartCoroutine(ConnectToURSim());
        // 0.1초마다 데이터 중계 루프 실행
        InvokeRepeating(nameof(BridgeDataLoop), 1f, 0.1f);
    }

    IEnumerator ConnectToURSim()
    {
        while (!_isConnected)
        {
            try
            {
                _tcpClient = new TcpClient(urIpAddress, port);
                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateMaster(_tcpClient);
                _isConnected = true;
                Debug.Log("urSim Modbus 연결 성공!");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"urSim 연결 대기 중... : {e.Message}");
                yield return new WaitForSeconds(2f);
            }
        }
    }

    void BridgeDataLoop()
    {
        if (!_isConnected || IO_Manager.Instance == null) return;

        try
        {
            // --- 1. urSim(130) -> PLC(M130) 전달 ---
            // Digital Output은 보통 Coils(0번대 주소)로 읽습니다.
            bool urJobDone = _modbusMaster.ReadCoils(1, 130, 1)[0];
            IO_Manager.Instance.SetOutput(plcJobDoneBit, urJobDone);

            // --- 2. PLC(D128) -> urSim(128) 전달 ---
            short plcTargetCV = IO_Manager.Instance.GetRegister("D128");
            _modbusMaster.WriteSingleRegister(1, urTargetAddress, (ushort)plcTargetCV);

            // 데이터 변경 확인용 로그 (필요 시 주석 해제)
            // Debug.Log($"Bridge: PLC {plcTargetCV} -> urSim {urTargetAddress} | urSim 130 -> PLC {plcJobDoneBit} ({urJobDone})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"통신 중계 오류: {e.Message}");
            _isConnected = false;
            StartCoroutine(ConnectToURSim());
        }
    }

    void OnDestroy()
    {
        _tcpClient?.Close();
    }
}