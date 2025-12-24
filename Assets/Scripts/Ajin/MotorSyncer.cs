using UnityEngine;

public class MotorSyncer : MonoBehaviour
{
    [Header("Axis Settings")]
    public int axisNo = 0;          // 읽어올 아진 축 번호
    public float unitScale = 0.001f; // 아진 좌표(mm 또는 pulse)를 유니티 단위로 변환하는 비율

    [Header("Movement Axis")]
    public bool moveX = true;       // 어느 축으로 움직일지 선택
    public bool moveY = false;
    public bool moveZ = false;

    public float xOffset = 0f;
    public float yOffset = 0f;
    public float zOffset = 0.3f;

    private double currentPos = 0;

    void Update()
    {
        // 1. AjinextekManager가 초기화되었는지 확인
        if (AjinextekManager.Instance == null) return;

        // 2. 아진 API: 현재 실제 위치(Actual Position) 읽기
        // 성공 시 0(AXT_RT_SUCCESS)을 반환합니다.
        uint ret = CAXM.AxmStatusGetActPos(axisNo, ref currentPos);

        if (ret == 0)
        {
            // 3. 읽어온 좌표값을 유니티 좌표로 변환
            float translatedPos = (float)currentPos * unitScale;

            // 4. 유니티 오브젝트의 위치 업데이트
            Vector3 newPos = transform.localPosition;

            if (moveX) newPos.x = translatedPos  + xOffset;
            if (moveY) newPos.y = translatedPos  + yOffset;
            if (moveZ) newPos.z = translatedPos  + zOffset;

            transform.localPosition = newPos;
        }
    }
}