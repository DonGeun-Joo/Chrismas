using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
using Unity.VisualScripting;
using System.Threading.Tasks;

public class URDataReceiver : MonoBehaviour
{
    [Header("Connection Settings")]
    public string urSimIP = "192.168.0.15";
    public int port = 30003;
    public int connectionTimeoutMs = 1000; // 1초 타임아웃
    public int reconnectIntervalMs = 2000; // 실패 시 2초 뒤 재시도

    [Header("Joint Objects")]
    public Transform[] jointTransforms;

    [Header("Status (Read Only)")]
    public bool isConnected = false;

    [Header("Joint Offset")]
    // 유니티와 UR의 좌표축 차이를 보정하기 위한 오프셋 (필요 시 조절)
    // 예: 모델이 누워있다면 x축에 90도 등을 넣어야 함
    public Vector3 baseOffset = new Vector3(0, 0, 0);
    public Vector3 shoulderOffset = new Vector3(0, 0, 0);
    public Vector3 elboeOffset = new Vector3(0, 0, 0);
    public Vector3 wrist1Offset = new Vector3(0, 0, 0);
    public Vector3 wrist2Offset = new Vector3(0, 0, 0);
    public Vector3 wrist3Offset = new Vector3(0, 0, 0);

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;

    // 스레드 안전성을 위해 lock 객체 사용
    private object dataLock = new object();
    private double[] targetJointPositions = new double[6];

    async void Start()
    {
        // 안전을 위해 배열 초기화
        for (int i = 0; i < 6; i++) targetJointPositions[i] = 0.0;

        // 비동기 연결 시도
        await ConnectionLifecycle();
    }

    // 연결의 전체 수명 주기를 관리하는 핵심 루프
    async Task ConnectionLifecycle()
    {
        while (this != null) // 게임 오브젝트가 살아있는 동안 계속 반복
        {
            if (!isConnected)
            {
                // 연결이 안 되어 있다면 연결 시도
                bool success = await TryConnectAsync();
                if (success)
                {
                    Debug.Log("<color=green>URSim Connected!</color>");
                    isConnected = true;

                    // 데이터 수신 스레드 시작
                    isRunning = true;
                    receiveThread = new Thread(ReceiveData);
                    receiveThread.IsBackground = true;
                    receiveThread.Start();
                }
                else
                {
                    // 연결 실패 시 잠시 대기 후 루프 처음으로 (재시도)
                    // Debug.Log("Retrying in 2 seconds..."); 
                    await Task.Delay(reconnectIntervalMs);
                }
            }
            else
            {
                // 이미 연결되어 있다면? 잘 붙어있는지 감시만 함
                // TCP는 연결이 끊겨도 데이터를 보내보기 전엔 모르는 경우가 많음.
                // ReceiveData 스레드에서 끊김을 감지하면 isConnected를 false로 바꿀 것임.
                await Task.Delay(1000);
            }
        }
    }

    async Task<bool> TryConnectAsync()
    {
        // 기존 리소스 정리
        if (client != null) client.Close();
        client = new TcpClient();

        try
        {
            var connectTask = client.ConnectAsync(urSimIP, port);
            var timeoutTask = Task.Delay(connectionTimeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // 타임아웃
                return false;
            }
            else
            {
                // 연결 성공했으나 예외 확인
                await connectTask;
                stream = client.GetStream();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[2048]; // 넉넉하게 잡음

        while (isRunning)
        {
            try
            {
                if (client == null || !client.Connected || stream == null)
                {
                    throw new Exception("Client disconnected");
                }

                // [연결 끊김 체크 보강]
                // 읽을 데이터가 없는데 소켓 상태가 이상하면 끊긴 것
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] check = new byte[1];
                    if (client.Client.Receive(check, SocketFlags.Peek) == 0)
                    {
                        throw new Exception("Socket closed remotely");
                    }
                }

                // 1. 패킷의 전체 길이를 알기 위해 앞의 4바이트(Integer)만 먼저 읽습니다.
                byte[] sizeBuffer = new byte[4];
                int bytesRead = stream.Read(sizeBuffer, 0, 4);
                if (bytesRead < 4) continue;

                // Big Endian(UR) -> Little Endian(C#) 변환
                if (BitConverter.IsLittleEndian) Array.Reverse(sizeBuffer);
                int packetSize = BitConverter.ToInt32(sizeBuffer, 0);

                // 2. 패킷 크기가 비정상적으로 작거나 크면 무시 (URSim 3.x~5.x는 보통 1044~1116 바이트)
                if (packetSize < 800 || packetSize > 2000) continue;

                // 3. 나머지 데이터를 정확히 packetSize - 4 만큼 읽습니다.
                byte[] packetBuffer = new byte[packetSize - 4];
                int totalRead = 0;
                while (totalRead < packetBuffer.Length)
                {
                    int read = stream.Read(packetBuffer, totalRead, packetBuffer.Length - totalRead);
                    if (read == 0) break; // 연결 끊김
                    totalRead += read;
                }

                // 4. 데이터 파싱 (Actual Joint Positions)
                // 전체 패킷 기준 Offset 252번이 관절 데이터 시작점입니다.
                // 우리는 앞의 4바이트(헤더)를 따로 읽었으므로, 현재 버퍼에서는 252 - 4 = 248번 인덱스입니다.
                int dataOffset = 248;

                // 데이터 파싱 (Offset 248)
                if (packetBuffer.Length >= dataOffset + 48)
                {
                    double[] parsed = new double[6];
                    for (int i = 0; i < 6; i++)
                    {
                        byte[] val = new byte[8];
                        Array.Copy(packetBuffer, dataOffset + (i * 8), val, 0, 8);
                        if (BitConverter.IsLittleEndian) Array.Reverse(val);
                        parsed[i] = BitConverter.ToDouble(val, 0);
                    }
                    lock (dataLock)
                    {
                        Array.Copy(parsed, targetJointPositions, 6);
                    }
                }
            }
            catch (Exception e)
            {
                // 예외 발생 시 연결 끊김 처리
                isRunning = false; 
                isConnected = false; // 메인 루프가 이걸 보고 다시 연결 시도를 시작함
                Debug.LogWarning("연결 실패 이유: " + e.Message);
                break;
            }
        }
    }

    void Update()
    {
        if (!isConnected) return; // 연결 안되어 있으면 업데이트 안 함

        if (jointTransforms == null || jointTransforms.Length < 6) return;

        double[] currentJoints;
        lock (dataLock)
        {
            // 스레드 충돌 방지를 위해 복사해서 사용
            currentJoints = (double[])targetJointPositions.Clone();
        }

        for (int i = 0; i < 6; i++)
        {
            // 라디안 -> 도 변환
            float angleDeg = (float)(currentJoints[i] * Mathf.Rad2Deg);

            // NaN 체크 (혹시 몰라 이중 체크)
            if (float.IsNaN(angleDeg)) continue;

            // [중요] 좌표축 보정
            // URSim의 관절 회전축이 유니티 모델의 어떤 축(X, Y, Z)과 매칭되는지 확인해야 합니다.
            // 일단 기본적으로 Z축 회전으로 가정하고 넣습니다.
            // 만약 로봇이 이상하게 꼬이면 아래 new Vector3(0, 0, angleDeg)를
            // new Vector3(0, angleDeg, 0) 또는 new Vector3(angleDeg, 0, 0) 등으로 바꿔보세요.
            switch (i)
            {
                case 0:
                    jointTransforms[i].localEulerAngles = new Vector3(0, -angleDeg, 0) + baseOffset;
                        break;
                case 1:
                    jointTransforms[i].localEulerAngles = new Vector3(0, 0, angleDeg) + shoulderOffset;
                    break;
                case 2:
                    jointTransforms[i].localEulerAngles = new Vector3(0, 0, angleDeg) + elboeOffset;
                        break;
                case 3:
                    jointTransforms[i].localEulerAngles = new Vector3(0, 0, angleDeg) + wrist1Offset;
                        break;
                case 4:
                    jointTransforms[i].localEulerAngles = new Vector3(angleDeg, 0, 0) + wrist2Offset;
                        break;
                case 5:
                    jointTransforms[i].localEulerAngles = new Vector3(0, 0, angleDeg) + wrist3Offset;
                        break;
                
            }
        }
    }

    void OnApplicationQuit()
    {
        // 종료 시 깔끔하게 정리
        isRunning = false;
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(500);
    }
}