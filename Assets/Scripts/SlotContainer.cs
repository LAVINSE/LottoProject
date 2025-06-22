using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotContainer : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int index = 0;
    [SerializeField] private List<Slot> SlotList = new();
    [SerializeField] private int result;
    [SerializeField] private int rotationCount;

    [Space]
    [SerializeField] private float spinSpeed = 1000f;       // 회전 속도
    [SerializeField] private float spinningTime = 2f;       // 최소 회전 시간
    [SerializeField] private float slowdownRate = 0.95f;    // 감속 비율
    [SerializeField] private float finalMoveSpeed = 50f;    // 최종 정렬 속도
    [SerializeField] private float stopThreshold = 0.1f;

    private bool isSpinning = false;
    private float currentSpeed;
    private Vector3 targetPosition;
    private bool lastResetState = false;
    // 마지막 회전때 결과값이 포함된 슬롯들을 가지고 있고, 해당 인덱스 위치로 천천히 이동

    #region 함수 
    [Button]
    public void CreateSlot(int createCount = 5)
    {
        foreach (var slot in SlotList)
        {
            if (slot == null)
                continue;
            DestroyImmediate(slot.gameObject);
        }

        SlotList.Clear();

        for (int i = 0; i < createCount; i++)
        {
            Slot slot = SwUtilsFactory.CreateCloneGameObj<Slot>("Slot", slotPrefab, this.gameObject, Vector3.zero, Vector3.one, Vector3.zero);

            SlotList.Add(slot);
        }
    }

    [Button]
    public void SetHeight()
    {
        float height = 0;
        float width = 0;
        foreach (var slot in SlotList)
        {
            height += slot.GetComponent<RectTransform>().rect.height;
            width = slot.GetComponent<RectTransform>().rect.width;
        }

        this.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

    [Button]
    public void SetPosY()
    {
        this.GetComponent<RectTransform>().anchoredPosition = GetResultSlotPos(index);
    }

    [Button]
    public void SetVertical()
    {
        int count = SlotList.Count;
        float slotHeight = SlotList.FirstOrDefault().GetComponent<RectTransform>().rect.height;

        float totalHeight = count * slotHeight + (count - 1) * 0;
        float startY = totalHeight / 2f - slotHeight / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform slot = SlotList[i].GetComponent<RectTransform>();
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.localScale = Vector3.one;

            float y = startY - i * (slotHeight);
            slot.anchoredPosition = new Vector2(0f, y);

        }
    }
    #endregion // 함수



    public Vector3 GetResultSlotPos(int index)
    {
        float height = 0;
        foreach (var slot in SlotList)
        {
            height += slot.GetComponent<RectTransform>().rect.height;
        }

        float slotHeight = height / SlotList.Count;
        float slotIndexHeight = ((slotHeight / 2) + (slotHeight * index)) - (height / 2);

        return new Vector3(0, slotIndexHeight, 0);
    }

    private int GetResultIndex()
    {
        Slot slot = SlotList.Find(x => x.Number == result);
        return SlotList.IndexOf(slot);
    }

    private bool IsFirstSlotPassedBottom()
    {
        // 현재 컨테이너의 위치
        float currentPosY = this.GetComponent<RectTransform>().anchoredPosition.y;

        // 전체 높이 계산
        float totalHeight = 0;
        foreach (var slot in SlotList)
        {
            totalHeight += slot.GetComponent<RectTransform>().rect.height;
        }

        // 슬롯 한 개의 높이 계산
        float slotHeight = totalHeight / SlotList.Count;

        // 0번째 슬롯의 하단 위치
        float firstSlotBottomPosY = GetResultSlotPos(0).y; //- (slotHeight);

        // 0번째 슬롯이 완전히 아래로 사라졌는지 확인
        return currentPosY <= firstSlotBottomPosY;
    }

    private void AllSlotRandomSetNumber()
    {
        foreach (var slot in SlotList)
        {
            slot.SetText(Random.Range(1, 51));
        }
    }

    private void AllSlotResultSetNumber()
    {
        int randomSlotIndex = Random.Range(0, SlotList.Count);

        for (int i = 0; i < SlotList.Count; i++)
        {
            Slot slot = SlotList[i];
            
            if (i == randomSlotIndex)
            {
                slot.SetText(result);
            }
            else
            {
                slot.SetText(Random.Range(1, 51));
            }
        }
    }

    private bool HasCompletedRotation()
    {
        bool currentResetState = IsFirstSlotPassedBottom();

        // 이전에는 리셋 상태가 아니었고, 현재는 리셋 상태인 경우 = 한 바퀴 회전 완료
        if (!lastResetState && currentResetState)
        {
            lastResetState = true;
            return true;
        }
        else if (lastResetState && !currentResetState)
        {
            // 리셋 상태에서 빠져나온 경우 상태 업데이트
            lastResetState = false;
        }

        return false;
    }

    [Button]
    private void Move()
    {
        if (!isSpinning)
        {
            StartCoroutine(MoveContainer());
        }
    }

    [Button]
    private void Stop()
    {
        StopCoroutine(MoveContainer());
    }

    private IEnumerator MoveContainer()
    {
        isSpinning = true;

        // 마지막 위치에서 시작
        SetPosY();

        // 초기에 랜덤 번호 설정
        AllSlotRandomSetNumber();

        // 회전 시작 시 초기 속도 설정
        currentSpeed = spinSpeed;

        // 최소 회전 시간 동안 빠르게 회전
        float spinTimer = 0;
        while (spinTimer < spinningTime)
        {
            // 컨테이너를 아래로 이동시켜 회전 효과를 만듦
            MoveContainerDown();

            if (IsFirstSlotPassedBottom())
            {
                // 마지막 슬롯 위치로 리셋
                this.GetComponent<RectTransform>().anchoredPosition = GetResultSlotPos(SlotList.Count - 1);

                rotationCount++;
                AllSlotRandomSetNumber();
            }

            spinTimer += Time.deltaTime;
            yield return null;
        }

        // 결과 슬롯 설정 (마지막 회전에서만)
        AllSlotResultSetNumber();

        // 결과 위치 계산
        int resultIndex = GetResultIndex();
        if (resultIndex < 0)
        {
            Debug.LogError("결과값이 슬롯에 존재하지 않습니다!");
            isSpinning = false;
            yield break;
        }

        // 결과 슬롯이 중앙에 오도록 하는 목표 위치 계산
        targetPosition = GetResultSlotPos(resultIndex);

        if (!IsFirstSlotPassedBottom())
        {
            while (true)
            {
                MoveContainerDown();
                if (IsFirstSlotPassedBottom())
                {
                    // 마지막 슬롯 위치로 리셋
                    this.GetComponent<RectTransform>().anchoredPosition = GetResultSlotPos(SlotList.Count - 1);
                    break;
                }

                yield return null;
            }
        }

        while (Vector3.Distance(GetComponent<RectTransform>().anchoredPosition, targetPosition) > stopThreshold)
        {
                // 목표 위치로 천천히 이동
                GetComponent<RectTransform>().anchoredPosition = Vector3.MoveTowards(
                    GetComponent<RectTransform>().anchoredPosition,
                    targetPosition,
                    finalMoveSpeed * Time.deltaTime
                );
                yield return null;
        }

        // 정확한 위치에 설정
        GetComponent<RectTransform>().anchoredPosition = targetPosition;

        // 결과 표시 또는 이벤트 발생 추가할 수 있음
        Debug.Log("슬롯 머신 정지! 결과: " + result);

        isSpinning = false;
    }

    // 컨테이너를 아래로 이동시키는 함수
    private void MoveContainerDown()
    {
        // 현재 위치 가져오기
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector3 currentPos = rectTransform.anchoredPosition;

        // Y 위치를 아래로 이동시킴
        currentPos.y -= currentSpeed * Time.deltaTime;

        // 업데이트된 위치 적용
        rectTransform.anchoredPosition = currentPos;
    }
}