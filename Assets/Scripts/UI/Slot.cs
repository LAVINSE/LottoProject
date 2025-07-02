using TMPro;
using UnityEngine;

public class Slot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private int number;

    public int Number => number;

    public void SetText(int number)
    {
        this.number = number;
        text.text = number.ToString();
    }
}
