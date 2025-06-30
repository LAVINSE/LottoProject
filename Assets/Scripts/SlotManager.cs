using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotManager : SwSingletonScene<SlotManager>
{
    #region 변수
    [SerializeField] private List<SlotContainer> slotContainerList = new();
    #endregion // 변수

    #region 프로퍼티
    #endregion // 프로퍼티

    #region 함수
    [Button]
    public void StartSlotMachine()
    {
        // 로직 모듈화
        LogicRandom();

        float delayStep = 0.3f;
        for (int i = 0; i < slotContainerList.Count; i++)
        {
            float stopDelay = delayStep * i;
            slotContainerList[i].Move(stopDelay);
        }
    }

    [Button]
    public void LogicRandom()
    {
        List<int> list = new();

        while (list.Count < slotContainerList.Count)
        {
            int random = Random.Range(1, 46);
            if (!list.Contains(random))
            {
                list.Add(random);
                Debug.Log(random);
            }
        }

        for (int i = 0; i < slotContainerList.Count; i++)
        {
            slotContainerList[i].Result = list[i];
        }
    }

    private void LogicRange(int start, int end)
    {
        List<LottoData> dataList = APIManager.Instance.LoadDrawDataRange(start, end);

        // 데이터가 없을때 처리해줘야함
        if (dataList.Count <= 0)
        {
            return;
        }
    }

    private void LogicRangeDate(string startDate, string endDate)
    {
        List<LottoData> dataList = APIManager.Instance.LoadDrawDataByDateRange(startDate, endDate);

        // 데이터가 없을때 처리해줘야함
        if (dataList.Count <= 0)
        {
            return;
        }
    }

    private void Logic(int number)
    {
        LottoData data = APIManager.Instance.LoadDrawData(number);

        // 데이터가 없을때 처리해줘야함
        if (data == null)
        {
            return;
        }
    }

    private void LogicDate(string date)
    {
        LottoData data = APIManager.Instance.LoadDrawDataByDate(date);

        // 데이터가 없을때 처리해줘야함
        if (data == null)
        {
            return;
        }
    }
    #endregion // 함수

    #region 유틸
#if UNITY_EDITOR
    [Button]
    private void FindSlotContainersAll()
    {
        SlotContainer[] containers = FindObjectsByType<SlotContainer>(FindObjectsSortMode.None);
        foreach (var container in containers)
        {
            int number = SwUtilsString.GetFirstStringNumber(container.name);
            container.SlotContainerIndex = number;
        }

        slotContainerList = containers.OrderBy(x => x.SlotContainerIndex).ToList();
    }
#endif // UNITY_EDITOR
    #endregion // 유틸
}
