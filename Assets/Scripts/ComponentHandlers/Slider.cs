using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FingerSliderPrefab : MonoBehaviour
{
    public Slider slider1;
    public Slider slider2;
    public Slider slider3;
    public TextMeshProUGUI dayText;

    private void Start()
    {
        slider1.interactable = false;
        slider2.interactable = false;
        slider3.interactable = false;
        dayText.color = Color.black;
    }

    public void changeSliderValue(float value1, float value2, float value3, string day)
    {
        slider1.value = value1/100;
        slider2.value = value2/100;
        slider3.value = value3/100;
        dayText.text = day;
        Debug.Log($"{(float)value1}");
        Debug.Log($"{(float)value2}");
        Debug.Log($"{(float)value3}");
    }
}
