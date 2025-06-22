using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LottoData
{
    public string returnValue; // 요청 결과 값
    public int drwNo; // 회차번호
    public string drwNoDate; // 추첨 날짜
    public long totSellamnt; // 총 판매금액
    public long firstWinamnt; // 1등 당첨자 1인당 금액
    public int firstPrzwnerCo; // 1등 당첨자 수
    public int drwtNo1; // 추첨번호
    public int drwtNo2; // 추첨번호
    public int drwtNo3; // 추첨번호
    public int drwtNo4; // 추첨번호
    public int drwtNo5; // 추첨번호
    public int drwtNo6; // 추첨번호
    public int bnusNo; // 보너스 번호 2등 판단
}

[System.Serializable]
public class LottoDataWrapper
{
    public List<LottoData> dataList = new();
}
