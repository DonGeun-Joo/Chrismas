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
    [SerializeField] private int _pointsPerSlot = 32;   // 슬롯당 점수 (기본 16점)

    // PLC 상태 미러링 (X, Y 주소 통합 관리)
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
        // 입력 주소 생성 (X0 ~ X3F)
        int totalInputPoints = _inputSlotCount * _pointsPerSlot;
        for (int i = 0; i < totalInputPoints; i++)
        {
            RegisterAddress("X" + i.ToString("X"));
        }

        int offset = totalInputPoints;
        int totalOutputPoints = _outputSlotCount * _pointsPerSlot;
        for (int i = 0; i < totalOutputPoints; i++)
        {
            RegisterAddress("Y" + (offset + i).ToString("X"));
        }
    }

    private void RegisterAddress(string addr)
    {
        string upperAddr = addr.ToUpper();
        _ioMirror[upperAddr] = 0;

        // MXRequester에 감시 등록: 값이 변할 때마다 _ioMirror 업데이트
        MXRequester.Get.AddDeviceAddress(upperAddr, (val) => {
            _ioMirror[upperAddr] = val;
        });
    }

    // --- [외부 장치용 공용 API] ---

    /// <summary>
    /// 입력(X) 또는 출력(Y)의 현재 상태를 확인합니다.
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
    /// PLC에 출력(Y) 신호를 보냅니다.
    /// </summary>
    public void SetOutput(string address, bool value)
    {
        short val = value ? (short)1 : (short)0;
        MXRequester.Get.AddSetDeviceRequest(address.ToUpper(), val);
    }
}