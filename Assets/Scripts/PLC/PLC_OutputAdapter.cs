using UnityEngine;
using UnityEngine.Events;

public class PLC_OutputAdapter : MonoBehaviour
{
    [Header("PLC 주소 설정")]
    public string plcAddress; // I/O 리스트의 출력 주소 (예: Y5, YA)
    public string commant;

    [Header("이벤트 설정")]
    [Tooltip("PLC 신호가 ON(1)이 될 때 실행될 함수를 연결하세요.")]
    public UnityEvent OnActivated;

    [Tooltip("PLC 신호가 OFF(0)이 될 때 실행될 함수를 연결하세요.")]
    public UnityEvent OnDeactivated;

    [Header("상태 모니터링")]
    [SerializeField] private bool _currentState = false;

    void Start()
    {
        if (string.IsNullOrEmpty(plcAddress))
        {
            Debug.LogError($"{gameObject.name}: PLC 주소가 설정되지 않았습니다!");
            return;
        }

        // MXRequester에 주소 감시 등록
        // IO_Manager가 이미 등록했더라도, 여기서 콜백을 추가로 등록하여 직접 신호를 받습니다.
        MXRequester.Get.AddDeviceAddress(plcAddress.ToUpper(), OnValueChanged);
        //Debug.Log($"{plcAddress.ToUpper()} : {commant}");
    }

    private void OnValueChanged(short value)
    {
        bool newState = (value != 0);

        // 값이 변했을 때만 이벤트 발생 (성능 최적화 및 중복 실행 방지)
        if (_currentState == newState) return;

        _currentState = newState;

        if (_currentState)
        {
            OnActivated.Invoke();
            //Debug.Log($"{commant} : On");
        }
        else
        {
            OnDeactivated.Invoke();
            //Debug.Log($"{commant} :Off");
        }
    }

    // 현재 상태를 외부 스크립트에서 확인할 수 있는 속성
    public bool IsOn => _currentState;
}