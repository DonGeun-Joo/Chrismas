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

    // ⭐ 중요: 주소가 추가된 순서를 저장하는 리스트 (인덱스 매칭용)
    private readonly List<string> _addressOrder = new List<string>();

    private bool _isListChanged = false;
    private bool _isDataUpdated = false;

    [Header("PLC Settings")]
    [SerializeField] private int _interval = 100;    // 통신 간격 (ms)
    [SerializeField] private int _stationNumber = 1; // MX Component 스테이션 번호
    [SerializeField] private bool _autoConnect = true;

    private void Awake()
    {
        _instance = this;
        // 100은 초기 용량(Capacity)입니다.
        _mxComponent = new MXInterface(_interval, 100, _stationNumber);
        if (_autoConnect) Open();
    }

    public void Open() => _mxComponent.Open();
    public void Close() => _mxComponent.Close();

    // 주소 감시 등록 (램프/센서/IO_Manager 등에서 호출)
    public void AddDeviceAddress(string address, Action<short> callback)
    {
        if (string.IsNullOrEmpty(address)) return;
        address = address.ToUpper();

        if (!_subscribers.TryGetValue(address, out var sub))
        {
            sub = new DeviceSubscriber(address);
            _subscribers.Add(address, sub);

            // ⭐ 중요: 새로운 주소가 들어오면 리스트 끝에 추가하여 순서를 보장함
            _addressOrder.Add(address);
            _isListChanged = true;
        }

        if (callback != null)
        {
            sub.callbacks += callback;
            callback(sub.ReadValue); // 현재 값 즉시 알려주기
        }
    }

    // 주소에서 문자(X, Y)와 숫자(16진수)를 분리하여 정렬 순서를 결정하는 함수
    private int ComparePLCAddresses(string a, string b)
    {
        // 1. 접두어(X, Y, M 등) 분리
        char typeA = a[0];
        char typeB = b[0];

        // 2. 접두어가 다르면 X -> Y -> M 순서로 정렬 (원하시는 순서대로 조정 가능)
        if (typeA != typeB)
        {
            return typeA.CompareTo(typeB);
        }

        // 3. 접두어가 같다면 뒤의 16진수 숫자를 추출하여 정수로 변환 후 비교
        try
        {
            int valA = Convert.ToInt32(a.Substring(1), 16);
            int valB = Convert.ToInt32(b.Substring(1), 16);
            return valA.CompareTo(valB);
        }
        catch
        {
            return a.CompareTo(b); // 변환 실패 시 기본 문자열 비교
        }
    }

    // --- [통신 엔진(MXInterface)에서 호출하는 콜백 함수들] ---
    public void OnReceivedGetDevice(MXInterface.GetDeviceRequest res) { _getResQueue.Enqueue(res); _isDataUpdated = true; }
    public void OnReceivedSetDevice(MXInterface.SetDeviceRequest res) { _setResQueue.Enqueue(res); _isDataUpdated = true; }
    public void OnReceiveReadDatas(MXInterface.ReadDeviceRequest res) { _readResQueue.Enqueue(res); _isDataUpdated = true; }

    private void Update()
    {
        // 1. 감시 주소 목록이 변경되었다면 정렬 후 엔진에 통보
        if (_isListChanged)
        {
            // ⭐ 핵심: 오브젝트 생성 순서와 상관없이 PLC 메모리 순서(X0, X1... Y20, Y21...)대로 재정렬
            _addressOrder.Sort(ComparePLCAddresses);

            // 정렬된 순서 그대로 엔진(MXInterface)의 요청 문자열을 갱신함
            _mxComponent.SetAutoReadDevice(_addressOrder);
            _isListChanged = false;

            Debug.Log($"<color=yellow>MXRequester: Address list re-sorted. Count: {_addressOrder.Count}</color>");
        }

        if (!_isDataUpdated) return;

        // 2. 개별 읽기 결과 배달 (GetDevice)
        while (_getResQueue.TryDequeue(out var res))
        {
            res.Callback?.Invoke(res.ReadData);
        }

        // 3. 일괄 읽기 결과 배달 (정렬된 _addressOrder와 PLC 결과 배열을 매칭)
        while (_readResQueue.TryDequeue(out var res))
        {
            // 이제 _addressOrder는 항상 X0, X1, X2... 순서로 정렬되어 있으므로
            // PLC에서 순서대로 보내준 res.ReadDatas와 인덱스가 100% 일치합니다.
            for (int i = 0; i < _addressOrder.Count; i++)
            {
                if (i < res.ReadDatas.Length)
                {
                    string targetAddr = _addressOrder[i];
                    _subscribers[targetAddr].ReadValue = res.ReadDatas[i];
                }
            }
        }

        // 4. 쓰기 요청 결과 배달 (SetDevice)
        while (_setResQueue.TryDequeue(out var res))
        {
            res.Callback?.Invoke(res.IsSuccess);
        }

        _isDataUpdated = false;
    }

    public void AddSetDeviceRequest(string address, short value, Action<bool> callback = null)
    {
        _mxComponent.AddSetDeviceRequest(new MXInterface.SetDeviceRequest(address, value, callback));
    }

    private void OnApplicationQuit() => Close();
    private void OnDestroy() => _mxComponent?.Dispose();
}