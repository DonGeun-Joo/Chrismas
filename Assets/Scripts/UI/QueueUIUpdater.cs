using UnityEngine;
using TMPro;
using System.Text;

public class QueueUIUpdater : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI currentJobText;
    public TextMeshProUGUI nextJobText;
    public TextMeshProUGUI fullQueueList; // 세로 리스트로 표시될 텍스트
    public TextMeshProUGUI pickQueueList; // 세로 리스트로 표시될 텍스트

    [Header("Update Settings")]
    [Range(0.05f, 1.0f)]
    public float updateInterval = 0.1f;

    void Start()
    {
        InvokeRepeating(nameof(UpdateUIFromPLC), 0.5f, updateInterval);
    }

    void UpdateUIFromPLC()
    {
        if (IO_Manager.Instance == null) return;

        // 1. 현재 작업 (D128)
        short currentCV = IO_Manager.Instance.GetRegister("D128");
        currentJobText.text = currentCV > 0 ? $"WORKING: CV {currentCV}" : "IDLE";

        // 2. 1단계(Full) 큐 (D1001~D1004)
        short[] fullQueue = IO_Manager.Instance.GetRegisterBlock("D1001", 4);
        fullQueueList.text = FormatQueueToVerticalList(fullQueue, "URGENT");

        // 3. 2단계(Pick) 큐 (D3001~D3004)
        short[] pickQueue = IO_Manager.Instance.GetRegisterBlock("D3001", 4);
        pickQueueList.text = FormatQueueToVerticalList(pickQueue, "NORMAL");

        // 4. 다음 작업 결정
        int nextCV = 0;
        if (fullQueue[0] > 0) nextCV = fullQueue[0];
        else if (pickQueue[0] > 0) nextCV = pickQueue[0];

        nextJobText.text = nextCV > 0 ? $"NEXT: CV {nextCV}" : "NEXT: NONE";
    }

    // 데이터를 세로 리스트 형태("\n")의 문자열로 변환
    private string FormatQueueToVerticalList(short[] queue, string label)
    {
        StringBuilder sb = new StringBuilder();
        // sb.AppendLine($"[{label}]"); // 제목을 넣고 싶으면 활성화하세요

        int count = 1;
        foreach (short cv in queue)
        {
            if (cv <= 0) continue;

            // "1. CV 2" 와 같은 형태로 아래로 한 줄씩 추가
            sb.AppendLine($"{count}. Conveyor {cv}");
            count++;
        }

        return sb.Length > 0 ? sb.ToString() : "EMPTY";
    }
}