using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ELogicType
{
    None,
    Most,
    TopNumber,
    Similar
}

public class SlotManager : SwSingletonScene<SlotManager>
{
    #region 변수
    [SerializeField] private ELogicType logicType = ELogicType.None;
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

    [Button("슬롯 회차번호 범위")]
    public void StartSlotMachineDrawNo(int start, int end = 0)
    {
        // 로직 모듈화
        LogicRange(start, end);

        float delayStep = 0.3f;
        for (int i = 0; i < slotContainerList.Count; i++)
        {
            float stopDelay = delayStep * i;
            slotContainerList[i].Move(stopDelay);
        }
    }

    [Button("슬롯 날짜 범위")]
    public void StartSlotMachineDate(string startData, string endDate = "")
    {
        // 로직 모듈화
        LogicRangeDate(startData, endDate);

        float delayStep = 0.3f;
        for (int i = 0; i < slotContainerList.Count; i++)
        {
            float stopDelay = delayStep * i;
            slotContainerList[i].Move(stopDelay);
        }
    }

    private List<int> SelectedNumber(List<LottoData> dataList)
    {
        switch (logicType)
        {
            case ELogicType.Most:
                Debug.Log("각 자리별로 가장 많이 나온 번호를 선택합니다.");
                return GetMostFrequentNumbers(dataList);
            case ELogicType.TopNumber:
                Debug.Log("각 자리별 상위 N개 번호 중에서 랜덤하게 선택합니다.");
                return GetRandomFromTopNumbers(dataList, Random.Range(1, 10));
            case ELogicType.Similar:
                Debug.Log("각 자리별 최빈 번호의 유사 번호를 랜덤 선택합니다.");
                return GetSimilarNumbers(dataList, Random.Range(1, 6));
            default:
                Debug.Log("각 자리별로 가장 많이 나온 번호를 선택합니다.");
                return GetMostFrequentNumbers(dataList);
        }
        //Debug.Log("각 자리별로 가장 적게 나온 번호를 선택합니다.");
        //GetLeastFrequentNumbers(dataList);
    }

    private void LogicRandom()
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
        List<LottoData> dataList = end != 0 ? APIManager.Instance.LoadDrawDataRange(start, end) : APIManager.Instance.LoadDrawDataRange(start);

        // 데이터가 없을때 처리해줘야함
        if (dataList.Count <= 0)
        {
            Debug.LogWarning("범위 내 로또 데이터가 없습니다.");
            LogicRandom();
            return;
        }

        List<int> selectedNumbers = SelectedNumber(dataList);

        for (int i = 0; i < slotContainerList.Count; i++)
        {
            slotContainerList[i].Result = selectedNumbers[i];
        }
    }

    private void LogicRangeDate(string startDate, string endDate = "")
    {
        List<LottoData> dataList = endDate != "" ? APIManager.Instance.LoadDrawDataByDateRange(startDate, endDate) : APIManager.Instance.LoadDrawDataByDateRange(startDate);

        // 데이터가 없을때 처리해줘야함
        if (dataList.Count <= 0)
        {
            Debug.LogWarning("범위 내 로또 데이터가 없습니다.");
            LogicRandom();
            return;
        }

        List<int> selectedNumbers = SelectedNumber(dataList);

        for (int i = 0; i < slotContainerList.Count; i++)
        {
            slotContainerList[i].Result = selectedNumbers[i];
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

    // 로또 데이터를 분석하여 가장 많이 나온 번호 6개를 선택합니다.
    private List<int> GetMostFrequentNumbers(List<LottoData> dataList)
    {
        Dictionary<int, int>[] positionFrequencies = new Dictionary<int, int>[6];

        for (int pos = 0; pos < 6; pos++)
        {
            positionFrequencies[pos] = new Dictionary<int, int>();
            for (int i = 1; i <= 45; i++)
            {
                positionFrequencies[pos][i] = 0;
            }
        }

        // 각 회차 데이터를 순회하며 자리별로 빈도 계산
        foreach (var data in dataList)
        {
            positionFrequencies[0][data.drwtNo1]++;
            positionFrequencies[1][data.drwtNo2]++;
            positionFrequencies[2][data.drwtNo3]++;
            positionFrequencies[3][data.drwtNo4]++;
            positionFrequencies[4][data.drwtNo5]++;
            positionFrequencies[5][data.drwtNo6]++;
        }

        List<int> selectedNumbers = new List<int>();

        Debug.Log($"{dataList.Count}회차 데이터 분석:");

        // 각 자리별로 가장 많이 나온 번호 선택
        for (int pos = 0; pos < 6; pos++)
        {
            var mostFrequent = positionFrequencies[pos]
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .First();

            selectedNumbers.Add(mostFrequent.Key);

            Debug.Log($"{pos + 1}번째 자리: {mostFrequent.Key} (등장횟수: {mostFrequent.Value})");
        }

        return selectedNumbers;
    }

    // 가장 적게 나온 번호 6개를 선택합니다.
    private List<int> GetLeastFrequentNumbers(List<LottoData> dataList)
    {
        Dictionary<int, int>[] positionFrequencies = new Dictionary<int, int>[6];

        for (int pos = 0; pos < 6; pos++)
        {
            positionFrequencies[pos] = new Dictionary<int, int>();
            for (int i = 1; i <= 45; i++)
            {
                positionFrequencies[pos][i] = 0;
            }
        }

        foreach (var data in dataList)
        {
            positionFrequencies[0][data.drwtNo1]++;
            positionFrequencies[1][data.drwtNo2]++;
            positionFrequencies[2][data.drwtNo3]++;
            positionFrequencies[3][data.drwtNo4]++;
            positionFrequencies[4][data.drwtNo5]++;
            positionFrequencies[5][data.drwtNo6]++;
        }

        List<int> selectedNumbers = new List<int>();

        Debug.Log($"{dataList.Count}회차 데이터 분석:");

        for (int pos = 0; pos < 6; pos++)
        {
            var leastFrequent = positionFrequencies[pos]
                .OrderBy(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .First();

            selectedNumbers.Add(leastFrequent.Key);

            Debug.Log($"{pos + 1}번째 자리: {leastFrequent.Key} (등장횟수: {leastFrequent.Value})");
        }

        return selectedNumbers;
    }

    // 상위 N개 번호 중에서 랜덤하게 6개를 선택합니다.
    private List<int> GetRandomFromTopNumbers(List<LottoData> dataList, int topN = 15)
    {
        Dictionary<int, int>[] positionFrequencies = new Dictionary<int, int>[6];

        for (int pos = 0; pos < 6; pos++)
        {
            positionFrequencies[pos] = new Dictionary<int, int>();
            for (int i = 1; i <= 45; i++)
            {
                positionFrequencies[pos][i] = 0;
            }
        }

        foreach (var data in dataList)
        {
            positionFrequencies[0][data.drwtNo1]++;
            positionFrequencies[1][data.drwtNo2]++;
            positionFrequencies[2][data.drwtNo3]++;
            positionFrequencies[3][data.drwtNo4]++;
            positionFrequencies[4][data.drwtNo5]++;
            positionFrequencies[5][data.drwtNo6]++;
        }

        List<int> selectedNumbers = new List<int>();

        Debug.Log($"자리별 상위 {topN}개 중 랜덤 선택 결과 - 총 {dataList.Count}회차 데이터 분석:");

        for (int pos = 0; pos < 6; pos++)
        {
            // 해당 자리의 상위 topN개 번호 추출
            var topNumbers = positionFrequencies[pos]
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(topN)
                .Select(pair => pair.Key)
                .ToList();

            // 상위 번호들 중에서 랜덤 선택
            int randomIndex = Random.Range(0, topNumbers.Count);
            int selectedNumber = topNumbers[randomIndex];

            selectedNumbers.Add(selectedNumber);

            Debug.Log($"{pos + 1}번째 자리: {selectedNumber} (상위 후보: [{string.Join(", ", topNumbers)}])");
        }

        return selectedNumbers;
    }

    // 가장 많이 등장한 번호들의 유사 번호를 랜덤 선택합니다.
    private List<int> GetSimilarNumbers(List<LottoData> dataList, int similarRange = 3)
    {
        Dictionary<int, int>[] positionFrequencies = new Dictionary<int, int>[6];

        for (int pos = 0; pos < 6; pos++)
        {
            positionFrequencies[pos] = new Dictionary<int, int>();
            for (int i = 1; i <= 45; i++)
            {
                positionFrequencies[pos][i] = 0;
            }
        }

        foreach (var data in dataList)
        {
            positionFrequencies[0][data.drwtNo1]++;
            positionFrequencies[1][data.drwtNo2]++;
            positionFrequencies[2][data.drwtNo3]++;
            positionFrequencies[3][data.drwtNo4]++;
            positionFrequencies[4][data.drwtNo5]++;
            positionFrequencies[5][data.drwtNo6]++;
        }

        List<int> selectedNumbers = new List<int>();

        Debug.Log($"{dataList.Count}회차 데이터 분석:");

        for (int pos = 0; pos < 6; pos++)
        {
            // 해당 자리의 최빈 번호 찾기
            var mostFrequent = positionFrequencies[pos]
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .First();

            int baseNumber = mostFrequent.Key;

            // 유사 번호 후보 생성
            List<int> similarNumbers = new List<int>();
            for (int offset = -similarRange; offset <= similarRange; offset++)
            {
                int candidate = baseNumber + offset;
                if (candidate >= 1 && candidate <= 45)
                {
                    similarNumbers.Add(candidate);
                }
            }

            // 유사 번호 중에서 랜덤 선택
            int randomIndex = Random.Range(0, similarNumbers.Count);
            int selectedNumber = similarNumbers[randomIndex];

            selectedNumbers.Add(selectedNumber);

            Debug.Log($"{pos + 1}번째 자리: 기준 {baseNumber} → 선택 {selectedNumber} (후보: [{string.Join(", ", similarNumbers)}])");
        }

        return selectedNumbers;
    }
    #endregion // 함수

    #region 유틸
#if UNITY_EDITOR
    //[Button]
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
