using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class IO_Manager : MonoBehaviour
{
    private static IO_Manager _instance;
    public static IO_Manager Instance => _instance;

    [Header("PLC Slot Settings")]
    [SerializeField] private int _inputSlotCount = 1;   // 입력 슬롯 수 (예: 2개면 32점)
    [SerializeField] private int _outputSlotCount = 1;  // 출력 슬롯 수 (예: 2개면 32점)
    [SerializeField] private int _pointsPerSlot = 32;   // 슬롯당 점수 (기본 32점 설정)

    [Header("Queue Monitoring Settings")]
    [SerializeField]
    private string[] _extraDAddresses = { "D128",
        "D1000", "D1001", "D1002", "D1003", "D1004",
        "D3000", "D3001", "D3002", "D3003", "D3004" };

    // PLC 상태 미러링 (X, Y, D 주소 통합 관리)
    private readonly Dictionary<string, short> _ioMirror = new Dictionary<string, short>();

    void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateAndRegisterIO();
    }

    private void GenerateAndRegisterIO()
    {
        // 1. 입력 주소 생성 및 등록 (X0 ~ X3F)
        int totalInputPoints = _inputSlotCount * _pointsPerSlot;
        for (int i = 0; i < totalInputPoints; i++)
        {
            RegisterAddress("X" + i.ToString("X"));
        }

        // 2. 출력 주소 생성 및 등록 (Y40 ~ Y7F 등 offset 고려)
        int offset = totalInputPoints;
        int totalOutputPoints = _outputSlotCount * _pointsPerSlot;
        for (int i = 0; i < totalOutputPoints; i++)
        {
            RegisterAddress("Y" + (offset + i).ToString("X"));
        }

        // 3. 작업 큐 및 로봇 타겟 주소(D) 등록
        foreach (string dAddr in _extraDAddresses)
        {
            RegisterAddress(dAddr);
        }
    }

    private void RegisterAddress(string addr)
    {
        string upperAddr = addr.ToUpper();
        _ioMirror[upperAddr] = 0;

        // MXRequester에 감시 등록: 값이 변할 때마다 _ioMirror 업데이트
        // MXRequester가 Word(D) 주소도 지원한다고 가정합니다.
        MXRequester.Get.AddDeviceAddress(upperAddr, (val) => {
            _ioMirror[upperAddr] = val;
        });
    }

    // --- [외부 장치용 공용 API] ---

    /// <summary>
    /// 입력(X) 또는 출력(Y)의 비트 상태를 확인합니다.
    /// </summary>
    public bool GetBit(string address)
    {
        if (_ioMirror.TryGetValue(address.ToUpper(), out short val))
        {
            return val != 0;
        }
        return false;
    }

    /// <summary>
    /// 데이터 레지스터(D)의 현재 값을 읽어옵니다. (D128 등)
    /// </summary>
    public short GetRegister(string address)
    {
        if (_ioMirror.TryGetValue(address.ToUpper(), out short val))
        {
            return val;
        }
        return 0;
    }

    /// <summary>
    /// 특정 범위의 레지스터 블록을 배열로 반환합니다. (큐 시각화용)
    /// </summary>
    public short[] GetRegisterBlock(string baseAddress, int count)
    {
        short[] result = new short[count];
        // baseAddress가 "D3001"이라면 D3001, D3002... 순으로 읽음
        string prefix = baseAddress.Substring(0, 1); // "D"
        int startIdx = int.Parse(baseAddress.Substring(1)); // 3001

        for (int i = 0; i < count; i++)
        {
            string currentAddr = prefix + (startIdx + i);
            result[i] = GetRegister(currentAddr);
        }
        return result;
    }

    /// <summary>
    /// PLC에 출력(Y) 또는 데이터(D) 신호를 보냅니다.
    /// </summary>
    public void SetOutput(string address, bool value)
    {
        short val = value ? (short)1 : (short)0;
        MXRequester.Get.AddSetDeviceRequest(address.ToUpper(), val);
    }

    /// <summary>
    /// PLC의 데이터 레지스터(D)에 직접 값을 씁니다.
    /// </summary>
    public void SetRegister(string address, short value)
    {
        MXRequester.Get.AddSetDeviceRequest(address.ToUpper(), value);
    }
}