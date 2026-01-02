using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

public class MXRequester : MonoBehaviour
{
    // --- [내부 클래스: 주소별 구독자] ---
    [Serializable]
    public class DeviceSubscriber
    {
        public string address;
        public Action<short> callbacks;
        private short _readValue;

        public short ReadValue
        {
            get => _readValue;
            set
            {
                if (_readValue == value) return; // 값이 변했을 때만 실행 (깜빡임 방지)
                _readValue = value;
                callbacks?.Invoke(value);
            }
        }
        public DeviceSubscriber(string address) => this.address = address;
    }

    // --- [싱글톤 및 필드] ---
    private static MXRequester _instance;
    public static MXRequester Get => _instance;

    private MXInterface _mxComponent;

    // 비동기 큐 (엔진 -> 유니티 메인 스레드 전달용)
    private readonly ConcurrentQueue<MXInterface.GetDeviceRequest> _getResQueue = new();
    private readonly ConcurrentQueue<MXInterface.SetDeviceRequest> _setResQueue = new();
    private readonly ConcurrentQueue<MXInterface.ReadDeviceRequest> _readResQueue = new();

    private readonly Dictionary<string, DeviceSubscriber> _subscribers = new();
    private bool _isListChanged = false;
    private bool _isDataUpdated = false;

    [Header("PLC Settings")]
    [SerializeField] private int _interval = 100;    // 통신 간격 (ms)
    [SerializeField] private int _stationNumber = 1; // MX Component 스테이션 번호
    [SerializeField] private bool _autoConnect = true;

    private void Awake()
    {
        _instance = this;
        _mxComponent = new MXInterface(_interval, 100, _stationNumber);
        if (_autoConnect) Open();
    }

    public void Open() => _mxComponent.Open();
    public void Close() => _mxComponent.Close();

    // 주소 감시 등록 (램프/센서 등에서 호출)
    public void AddDeviceAddress(string address, Action<short> callback)
    {
        if (string.IsNullOrEmpty(address)) return;
        address = address.ToUpper();

        if (!_subscribers.TryGetValue(address, out var sub))
        {
            sub = new DeviceSubscriber(address);
            _subscribers.Add(address, sub);
            _isListChanged = true;
        }

        if (callback != null)
        {
            sub.callbacks += callback;
            callback(sub.ReadValue); // 현재 값 즉시 알려주기
        }
    }

    // --- [통신 엔진에서 호출하는 콜백 함수들] ---
    public void OnReceivedGetDevice(MXInterface.GetDeviceRequest res) { _getResQueue.Enqueue(res); _isDataUpdated = true; }
    public void OnReceivedSetDevice(MXInterface.SetDeviceRequest res) { _setResQueue.Enqueue(res); _isDataUpdated = true; }
    public void OnReceiveReadDatas(MXInterface.ReadDeviceRequest res) { _readResQueue.Enqueue(res); _isDataUpdated = true; }

    private void Update()
    {
        // 1. 감시 주소 목록이 변경되었다면 엔진에 통보
        if (_isListChanged)
        {
            var sortedList = _subscribers.Keys.OrderBy(a => a).ToList();
            _mxComponent.SetAutoReadDevice(sortedList);
            _isListChanged = false;
        }

        if (!_isDataUpdated) return;

        // 2. 개별 읽기 결과 배달
        while (_getResQueue.TryDequeue(out var res)) res.Callback?.Invoke(res.ReadData);

        // 3. 일괄 읽기 결과 배달 (가장 중요)
        while (_readResQueue.TryDequeue(out var res))
        {
            // Dictionary 키 정렬 순서와 res.ReadDatas의 인덱스 순서가 일치함
            var sortedKeys = _subscribers.Keys.OrderBy(a => a).ToList();
            for (int i = 0; i < sortedKeys.Count; i++)
            {
                if (i < res.ReadDatas.Length)
                    _subscribers[sortedKeys[i]].ReadValue = res.ReadDatas[i];
            }
        }

        // 4. 쓰기 요청 결과 배달
        while (_setResQueue.TryDequeue(out var res)) res.Callback?.Invoke(res.IsSuccess);

        _isDataUpdated = false;
    }

    public void AddSetDeviceRequest(string address, short value, Action<bool> callback = null)
    {
        _mxComponent.AddSetDeviceRequest(new MXInterface.SetDeviceRequest(address, value, callback));
    }

    private void OnApplicationQuit() => Close();
    private void OnDestroy() => _mxComponent?.Dispose();
}