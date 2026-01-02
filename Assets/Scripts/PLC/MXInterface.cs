using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ActUtlType64Lib; // MX Component 라이브러리
using UnityEngine;

public sealed class MXInterface : IDisposable
{
    // --- [데이터 구조체 정의] ---
    public struct SetDeviceRequest
    {
        public string DeviceAddress;   // PLC 주소 (예: "M0")
        public short WriteValue;       // 쓸 값 (0 또는 1)
        public Action<bool> Callback;  // 완료 후 실행할 콜백
        public bool IsSuccess;         // 성공 여부 결과

        public SetDeviceRequest(string address, short value, Action<bool> callback = null)
        {
            DeviceAddress = address;
            WriteValue = value;
            Callback = callback;
            IsSuccess = false;
        }
    }

    public struct GetDeviceRequest
    {
        public string DeviceAddress;
        public Action<short> Callback;
        public short ReadData;

        public GetDeviceRequest(string address, Action<short> callback = null)
        {
            DeviceAddress = address;
            Callback = callback;
            ReadData = 0;
        }
    }

    public struct ReadDeviceRequest
    {
        public short[] ReadDatas; // 일괄 읽기 결과 배열
        public ReadDeviceRequest(short[] datas) => ReadDatas = datas;
    }

    // --- [내부 변수] ---
    private readonly Thread _worker;              // 통신 전담 스레드
    private readonly AutoResetEvent _resetEvent;  // 스레드 제어용 이벤트
    private ActUtlType64 _communicator;           // MX Component 객체

    private readonly int _interval;               // 통신 주기
    private readonly int _stationNumber;          // 스테이션 번호
    private readonly string _password;            // 비밀번호

    private volatile bool _isRunning = false;     // 루프 제어 플래그
    private int _autoReadCount;                   // 읽을 주소 개수
    private short[] _autoReadDatas;               // 읽은 데이터 저장 배열
    private string _currentReadRequestString;     // 합쳐진 주소 문자열 ("M0\nM1...")

    // 스레드 안전한 요청 큐
    private readonly ConcurrentQueue<SetDeviceRequest> _setRequestQueue = new();
    private readonly ConcurrentQueue<GetDeviceRequest> _getRequestQueue = new();

    public MXInterface(int interval, int capacity, int stationNumber, string password = null)
    {
        _interval = interval;
        _stationNumber = stationNumber;
        _password = password;
        _autoReadDatas = new short[capacity];
        _resetEvent = new AutoResetEvent(false);

        // 스레드 설정
        _worker = new Thread(Run);
        _worker.IsBackground = true;
        _worker.SetApartmentState(ApartmentState.STA); // COM 객체 사용을 위한 필수 설정
    }

    public void Open() => _worker.Start();

    public void Close()
    {
        _isRunning = false;
        _resetEvent.Set(); // 대기 중인 스레드 깨우기
    }

    public void AddSetDeviceRequest(SetDeviceRequest request)
    {
        _setRequestQueue.Enqueue(request);
        _resetEvent.Set();
    }

    public void AddGetDeviceRequest(GetDeviceRequest request)
    {
        _getRequestQueue.Enqueue(request);
        _resetEvent.Set();
    }

    // 일괄 감시할 주소 리스트 설정
    public void SetAutoReadDevice(IEnumerable<string> devices)
    {
        var deviceList = devices.ToList();
        _autoReadCount = deviceList.Count;

        if (_autoReadCount > _autoReadDatas.Length)
            _autoReadDatas = new short[_autoReadCount * 2];

        // 주소들을 줄바꿈(\n)으로 합쳐서 MX Component 형식에 맞춤
        _currentReadRequestString = string.Join("\n", deviceList);
        _resetEvent.Set();
    }

    // --- [실제 통신 루프] ---
    private void Run()
    {
        try
        {
            _communicator = new ActUtlType64();
            _communicator.ActLogicalStationNumber = _stationNumber;
            if (!string.IsNullOrEmpty(_password)) _communicator.ActPassword = _password;

            int ret = _communicator.Open();
            if (ret == 0) Debug.Log("<color=green>PLC Connected Successfully</color>");
            else Debug.LogError($"PLC Connection Failed: 0x{ret:X8}");
        }
        catch (COMException e)
        {
            Debug.LogError($"COM Object Creation Failed: {e.Message}");
            return;
        }

        _isRunning = true;

        while (_isRunning)
        {
            // 요청이 없으면 대기, 있으면 주기만큼 대기
            if (string.IsNullOrEmpty(_currentReadRequestString)) _resetEvent.WaitOne();
            else _resetEvent.WaitOne(_interval);

            if (!_isRunning) break;

            // 1. 쓰기 요청 처리
            while (_setRequestQueue.TryDequeue(out var setReq))
            {
                int ret = _communicator.SetDevice2(setReq.DeviceAddress, setReq.WriteValue);
                setReq.IsSuccess = (ret == 0);
                MXRequester.Get.OnReceivedSetDevice(setReq); // 유니티 매니저에 알림
            }

            // 2. 개별 읽기 요청 처리
            while (_getRequestQueue.TryDequeue(out var getReq))
            {
                int ret = _communicator.GetDevice2(getReq.DeviceAddress, out getReq.ReadData);
                if (ret == 0) MXRequester.Get.OnReceivedGetDevice(getReq);
            }

            // 3. 일괄 랜덤 읽기 (핵심 - 렉 방지 및 다중 감시)
            if (!string.IsNullOrEmpty(_currentReadRequestString))
            {
                int ret = _communicator.ReadDeviceRandom2(_currentReadRequestString, _autoReadCount, out _autoReadDatas[0]);
                if (ret == 0)
                {
                    short[] copy = new short[_autoReadCount];
                    Array.Copy(_autoReadDatas, copy, _autoReadCount);
                    MXRequester.Get.OnReceiveReadDatas(new ReadDeviceRequest(copy));
                }
            }
        }

        if (_communicator != null) _communicator.Close();
    }

    public void Dispose()
    {
        Close();
        _resetEvent?.Dispose();
        GC.SuppressFinalize(this);
    }
}