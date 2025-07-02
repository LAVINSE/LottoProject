using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadingManager : SwSingleton<LoadingManager>
{
    #region 변수
    [SerializeField] private GameObject loadingObj;
    #endregion // 변수

    #region 프로퍼티
    #endregion // 프로퍼티

    #region 함수
    public void ShowLoading()
    {
        loadingObj.SetActive(true);
    }

    public void HideLoading()
    {
        loadingObj.SetActive(false);
    }

    public async UniTask ShowDuring(Func<UniTask> task)
    {
        ShowLoading();
        await task();
        HideLoading();
    }
    #endregion // 함수

    #region 유틸
#if UNITY_EDITOR
#endif // UNITY_EDITOR
    #endregion // 유틸
}
