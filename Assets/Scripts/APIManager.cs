using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

using System.Collections.Generic;
using System.Linq;
using System.Text;




#if UNITY_EDITOR
using UnityEditor;
#endif

public class APIManager : SwSingletonScene<APIManager>
{
    #region 변수
    [SerializeField, ReadOnly] private string fileName = "LottoDataAll.Json";
    [SerializeField] private string url = string.Empty;

    [SerializeField, ReadOnly] private string filePath = string.Empty;
    #endregion // 변수

    #region 함수
    [Button("API 데이터 가져오기 (로딩)")]
    public void GetAPILoadData(Action onComplete = null, Action<string> onError = null)
    {
        StartCoroutine(GetAPILoadDataRoutine(onComplete, onError));
    }

    [Button("API 데이터 가져오기 (단일)")]
    public void GetLottoData(int drawNumber, Action<LottoData> onSuccess = null, Action<string> onFail = null)
    {
        StartCoroutine(GetLottoDataRoutine(drawNumber, onSuccess : (data) =>
        {
            SaveJsonToDataFile(drawNumber, data);
        }, onFail));
    }

    [Button("API 데이터 가져오기 (범위)")]
    public void GetLottoDataRange(int startDraw, int endDraw, Action<LottoData> onSuccess = null, Action<int, string> onFail = null)
    {
        StartCoroutine(GetLottoDataRangeRoutine(startDraw, endDraw, onSuccess, onFail));
    }

    [Button("Json 데이터 가져오기 (단일)")]
    public LottoData LoadDrawData(int drawNumber)
    {
        LottoDataWrapper wrapper = LoadAllSavedData();

        if (wrapper == null) return null;

        foreach (var data in wrapper.dataList)
        {
            if (data.drwNo == drawNumber)
            {
                SwUtilsLog.Log($"[{drawNumber}회차] 데이터 로드 완료");
                return data;
            }
        }

        SwUtilsLog.LogWarning($"[{drawNumber}회차] 저장된 데이터가 없습니다.");
        return null;
    }

    [Button("Json 데이터 가져오기 (범위)")]
    public List<LottoData> LoadDrawDataRange(int startDraw, int endDraw)
    {
        LottoDataWrapper wrapper = LoadAllSavedData();
        List<LottoData> result = new();

        if (wrapper == null) return result;

        foreach (var data in wrapper.dataList)
        {
            if (data.drwNo >= startDraw && data.drwNo <= endDraw)
            {
                result.Add(data);
            }
        }

        // 회차 순으로 정렬
        result.Sort((a, b) => a.drwNo.CompareTo(b.drwNo));

        SwUtilsLog.Log($"{startDraw}-{endDraw}회차 데이터 로드 완료: {result.Count}개");
        return result;
    }

    private IEnumerator GetAPILoadDataRoutine(Action onComplete, Action<string> onError)
    {
#if UNITY_EDITOR
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif // UNITY_EDITOR

        SwUtilsLog.Log("API 데이터 로드 시작");

        LottoDataWrapper wrapper = LoadAllSavedData();

        int firstDrawNumber = 1;
        int lottoAPILastNumber = -1;

        // 먼저 마지막 회차 번호를 가져오기
        yield return StartCoroutine(GetLastDrawNumberRoutine(
            (lastNumber) => lottoAPILastNumber = lastNumber,
            (error) => onError?.Invoke($"마지막 회차 조회 실패: {error}")
        ));

        if (lottoAPILastNumber == -1)
        {
            onError?.Invoke("마지막 회차 번호를 가져올 수 없습니다.");

#if UNITY_EDITOR
            SwUtilsLog.Log($"Load Error {stopwatch.ElapsedMilliseconds}ms");
#endif // UNITY_EDITOR
            yield break;
        }

        SwUtilsLog.Log($"API 마지막 회차: {lottoAPILastNumber}회차");

        int startDrawNumber;
        int endDrawNumber = lottoAPILastNumber;

        if (wrapper == null)
        {
            startDrawNumber = firstDrawNumber;
            SwUtilsLog.Log($"저장된 데이터가 없어서 1회차부터 {lottoAPILastNumber}회차까지 가져옵니다.");
        }
        else
        {
            int fileLastDrawNumber = GetLastSavedDrawNumber(wrapper);
            SwUtilsLog.Log($"저장된 마지막 회차: {fileLastDrawNumber}회차");

            startDrawNumber = fileLastDrawNumber + 1;

            if (startDrawNumber > lottoAPILastNumber)
            {
                SwUtilsLog.Log("모든 데이터가 이미 최신 상태입니다.");
                onComplete?.Invoke();
#if UNITY_EDITOR
                SwUtilsLog.Log($"Load : {nameof(GetAPILoadDataRoutine)} - {stopwatch.ElapsedMilliseconds}ms");
#endif // UNITY_EDITOR
                yield break;
            }

            SwUtilsLog.Log($"{startDrawNumber}회차부터 {lottoAPILastNumber}회차까지 새 데이터를 가져옵니다.");
        }

        SwUtilsLog.Log($"Load : {nameof(GetAPILoadDataRoutine)} - {stopwatch.ElapsedMilliseconds}ms");
        // 병렬 처리로 데이터 수집
        yield return StartCoroutine(GetLottoDataRangeRoutine(
            startDrawNumber,
            endDrawNumber,
            null,
            null
        ));
    }

    private IEnumerator GetLottoDataRoutine(int drawNumber, Action<LottoData> onSuccess, Action<string> onFail)
    {
#if UNITY_EDITOR
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif // UNITY_EDITOR
        string finalUrl = string.Format(this.url, drawNumber);
        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SwUtilsLog.LogError($"[{drawNumber}회차] API 요청 실패: {request.error}");
                onFail?.Invoke(request.error);
            }
            else
            {
                SwUtilsLog.Log($"요청 성공: {request.result}");

                string json = request.downloadHandler.text;

                LottoData data = JsonUtility.FromJson<LottoData>(json);
                if (data.returnValue != "success")
                {
                    onFail?.Invoke($"유효하지 않은 회차이거나 API 실패 ({drawNumber}회차)");
                    SwUtilsLog.LogError($"유효하지 않은 회차이거나 API 실패 ({drawNumber}회차)");

#if UNITY_EDITOR
                    SwUtilsLog.Log($"Load Error {stopwatch.ElapsedMilliseconds}ms");
#endif // UNITY_EDITOR
                    yield break;
                }

                SwUtilsLog.Log($"[{drawNumber}회차] 당첨 번호: {data.drwtNo1}, {data.drwtNo2}, {data.drwtNo3}, {data.drwtNo4}, {data.drwtNo5}, {data.drwtNo6} + 보너스 {data.bnusNo}");

                //SaveJsonToFile(drawNumber, json);
                onSuccess?.Invoke(data);
#if UNITY_EDITOR
                SwUtilsLog.Log($"Load : {nameof(GetLottoDataRoutine)} - {stopwatch.ElapsedMilliseconds}ms");
#endif // UNITY_EDITOR
            }
        }
    }

    private IEnumerator GetLottoDataRangeRoutine(int startDraw, int endDraw, Action<LottoData> onSuccess, Action<int, string> onFail)
    {
#if UNITY_EDITOR
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif // UNITY_EDITOR
        SwUtilsLog.Log($"{startDraw}회차부터 {endDraw}회차까지 데이터 가져오기 시작");
        List<LottoData> dataList = new();
        for (int i = startDraw; i <= endDraw; i++)
        {
            yield return GetLottoDataRoutine(i,
                data =>
                {
                    SwUtilsLog.Log($"[{i}회차] 데이터 가져오기 완료");
                    dataList.Add(data);
                    onSuccess?.Invoke(data);
                },
                error => onFail?.Invoke(i, error)
            );
        }

        SwUtilsLog.Log($"{startDraw}회차부터 {endDraw}회차까지 데이터 가져오기 완료");
        SaveJsonToDataListFile(startDraw, endDraw, dataList);
#if UNITY_EDITOR
        SwUtilsLog.Log($"Load : {nameof(GetLottoDataRangeRoutine)} - {stopwatch.ElapsedMilliseconds}ms");
#endif // UNITY_EDITOR
    }

    private void SaveJsonToFile(int drawNumber, string newJson)
    {
        filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        // 기존 데이터 불러오기 또는 새로 생성
        LottoDataWrapper wrapper = new LottoDataWrapper();
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string existingJson = System.IO.File.ReadAllText(filePath);
                wrapper = JsonUtility.FromJson<LottoDataWrapper>(existingJson);
            }
            catch (Exception e)
            {
                SwUtilsLog.LogWarning($"기존 JSON 파싱 실패, 새로 생성합니다: {e.Message}");
                wrapper = new LottoDataWrapper();
            }
        }

        // 새 데이터 파싱
        LottoData newData = JsonUtility.FromJson<LottoData>(newJson);

        // 기존에 같은 회차가 있으면 제거
        wrapper.dataList.RemoveAll(d => d.drwNo == newData.drwNo);

        // 추가하고 정렬
        wrapper.dataList.Add(newData);
        wrapper.dataList.Sort((a, b) => a.drwNo.CompareTo(b.drwNo));

        // 다시 저장
        try
        {
            string finalJson = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(filePath, finalJson);
            SwUtilsLog.Log($"[{drawNumber}회차] JSON 저장 완료: {filePath}");
        }
        catch (Exception e)
        {
            SwUtilsLog.LogError($"[{drawNumber}회차] JSON 저장 실패: {e.Message}");
        }
    }

    private void SaveJsonToDataFile(int drawNumber, LottoData lottoData)
    {
        filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        // 기존 데이터 불러오기 또는 새로 생성
        LottoDataWrapper wrapper = new LottoDataWrapper();
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string existingJson = System.IO.File.ReadAllText(filePath);
                wrapper = JsonUtility.FromJson<LottoDataWrapper>(existingJson);
            }
            catch (Exception e)
            {
                SwUtilsLog.LogWarning($"기존 JSON 파싱 실패, 새로 생성합니다: {e.Message}");
                wrapper = null;
            }
        }

        // 기존에 같은 회차가 있으면 제거
        wrapper.dataList.RemoveAll(d => d.drwNo == lottoData.drwNo);

        // 추가하고 정렬
        wrapper.dataList.Add(lottoData);
        wrapper.dataList.Sort((a, b) => a.drwNo.CompareTo(b.drwNo));

        // 다시 저장
        try
        {
            string finalJson = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(filePath, finalJson);
            SwUtilsLog.Log($"[{drawNumber}회차] JSON 저장 완료: {filePath}");
        }
        catch (Exception e)
        {
            SwUtilsLog.LogError($"[{drawNumber}회차] JSON 저장 실패: {e.Message}");
        }
    }

    private void SaveJsonToDataListFile(int startDraw, int endDraw, List<LottoData> dataList)
    {
    {
        filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        // 기존 데이터 불러오기 또는 새로 생성
        LottoDataWrapper wrapper = new LottoDataWrapper();
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string existingJson = System.IO.File.ReadAllText(filePath);
                wrapper = JsonUtility.FromJson<LottoDataWrapper>(existingJson);
            }
            catch (Exception e)
            {
                SwUtilsLog.LogWarning($"기존 JSON 파싱 실패, 새로 생성합니다: {e.Message}");
                wrapper = new LottoDataWrapper();
            }
        }

            foreach (var data in dataList)
            {
                if (!wrapper.dataList.Contains(data))
                {
                    wrapper.dataList.Add(data);
                }
            }

        // 추가하고 정렬
        wrapper.dataList.Sort((a, b) => a.drwNo.CompareTo(b.drwNo));

        // 다시 저장
        try
        {
            string finalJson = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(filePath, finalJson);
            SwUtilsLog.Log($"[{startDraw}회차 ~ {endDraw}회차] JSON 저장 완료: {filePath}");
        }
        catch (Exception e)
        {
            SwUtilsLog.LogError($"[{startDraw}회차 ~ {endDraw}회차] JSON 저장 실패: {e.Message}");
        }
    }
}

    public LottoDataWrapper LoadAllSavedData()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        if (!System.IO.File.Exists(filePath))
        {
            SwUtilsLog.LogWarning("저장된 데이터 파일이 없습니다.");
            return null;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            LottoDataWrapper wrapper = JsonUtility.FromJson<LottoDataWrapper>(json);

            if (wrapper.dataList == null)
            {
                wrapper.dataList = new System.Collections.Generic.List<LottoData>();
            }

            SwUtilsLog.Log($"저장된 데이터 로드 완료: {wrapper.dataList.Count}개 회차");
            return wrapper;
        }
        catch (Exception e)
        {
            SwUtilsLog.LogError($"데이터 로드 실패: {e.Message}");
            return null;
        }
    }

    public bool HasSavedData()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        return System.IO.File.Exists(filePath);
    }

    public int GetSavedDrawCount(LottoDataWrapper wrapper)
    {
        return wrapper?.dataList?.Count ?? 0;
    }

    public (int minDraw, int maxDraw) GetSavedDrawRange(LottoDataWrapper wrapper)
    {
        if (wrapper?.dataList?.Count == 0)
        {
            return (0, 0);
        }

        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (var data in wrapper.dataList)
        {
            if (data.drwNo < min) min = data.drwNo;
            if (data.drwNo > max) max = data.drwNo;
        }

        return (min, max);
    }

    private IEnumerator GetLastDrawNumberRoutine(Action<int> onSuccess, Action<string> onError)
    {
        LottoData data = LoadDrawData(1);

        if (data == null)
        {
            SwUtilsLog.Log("1회차 데이터가 없어서 API에서 가져옵니다.");

            bool apiCallComplete = false;
            bool apiCallSuccess = false;

            yield return StartCoroutine(GetLottoDataRoutine(1,
                (lottoData) =>
                {
                    data = lottoData;
                    apiCallComplete = true;
                    apiCallSuccess = true;
                },
                (error) =>
                {
                    apiCallComplete = true;
                    apiCallSuccess = false;
                    onError?.Invoke($"1회차 데이터를 가져올 수 없습니다: {error}");
                }
            ));

            // API 호출 완료까지 대기
            yield return new WaitUntil(() => apiCallComplete);

            if (!apiCallSuccess)
            {
                yield break;
            }
        }

        string date = data.drwNoDate;

        try
        {
            System.DateTime firstTime = System.DateTime.Parse(date);
            System.DateTime now = System.DateTime.Now;

            SwUtilsLog.Log($"첫 번째 추첨일: {firstTime:yyyy-MM-dd}");
            SwUtilsLog.Log($"현재 시간: {now:yyyy-MM-dd}");

            // 경과 일수 계산
            TimeSpan elapsed = now - firstTime;
            SwUtilsLog.Log($"경과 일수: {elapsed.TotalDays:0}일");

            // 경과 주수 계산 (로또는 매주 토요일)
            int weeksPassed = (int)(elapsed.TotalDays / 7);
            SwUtilsLog.Log($"경과 주수: {weeksPassed}주");

            // 예상 현재 회차 계산
            int estimatedCurrentDraw = weeksPassed + 1;
            SwUtilsLog.Log($"예상 현재 회차: {estimatedCurrentDraw}회차");

            onSuccess?.Invoke(estimatedCurrentDraw);
        }
        catch (System.FormatException e)
        {
            onError?.Invoke($"날짜 형식 변환 실패: {e.Message}");
        }
    }

    public int GetLastSavedDrawNumber(LottoDataWrapper wrapper)
    {
        if (wrapper?.dataList == null || wrapper.dataList.Count == 0)
        {
            return 0; // 저장된 데이터가 없으면 0 반환
        }

        // 가장 높은 회차 번호 찾기
        int maxDrawNumber = wrapper.dataList[wrapper.dataList.Count - 1].drwNo;
      
        SwUtilsLog.Log($"저장된 마지막 회차: {maxDrawNumber}회차");
        return maxDrawNumber;
    }
    #endregion // 함수

    #region 유틸
#if UNITY_EDITOR
    [Button("마지막 회차 숫자 가져오기", 10f)]
    public void GetLastDrawNumber()
    {
        StartCoroutine(GetLastDrawNumberRoutine(
            (result) => SwUtilsLog.Log($"마지막 회차: {result}회차"),
            (error) => SwUtilsLog.LogError($"마지막 회차 조회 실패: {error}")
        ));
    }

    [Button("저장 폴더 열기")]
    public void OpenSaveFolder()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        string folder = System.IO.Path.GetDirectoryName(filePath);

        if (!System.IO.Directory.Exists(folder))
        {
            SwUtilsLog.LogWarning($"폴더가 존재하지 않습니다: {folder}");
            return;
        }

        EditorUtility.RevealInFinder(folder); // 폴더 열기
    }

    [Button("저장 파일 삭제")]
    public void DeleteSaveFile()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        if (!System.IO.File.Exists(filePath))
        {
            SwUtilsLog.LogWarning($"파일이 존재하지 않습니다: {filePath}");
            return;
        }

        try
        {
            System.IO.File.Delete(filePath);
            SwUtilsLog.Log($"저장 파일 삭제 완료: {filePath}");

            // Unity 에디터에서 변경사항 반영
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            SwUtilsLog.LogError($"파일 삭제 실패: {e.Message}");
        }
    }

    [Button("저장된 데이터 정보 출력")]
    public void PrintSavedDataInfo()
    {
        if (!HasSavedData())
        {
            SwUtilsLog.Log("저장된 데이터가 없습니다.");
            return;
        }

        LottoDataWrapper wrapper = LoadAllSavedData();
        int totalCount = GetSavedDrawCount(wrapper);
        var (minDraw, maxDraw) = GetSavedDrawRange(wrapper);
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"=== 저장된 데이터 정보 ===");
        stringBuilder.AppendLine($"총 회차 수: {totalCount}개");
        stringBuilder.AppendLine($"회차 범위: {minDraw}회차 ~ {maxDraw}회차");
        stringBuilder.AppendLine($"파일 경로: {filePath}");

        SwUtilsLog.Log(stringBuilder);
    }

    [Button("API 중지")]
    private void StopAPI()
    {
        StopAllCoroutines();
    }
#endif // UNITY_EDITOR
    #endregion // 유틸
}