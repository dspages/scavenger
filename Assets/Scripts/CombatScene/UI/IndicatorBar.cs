using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IndicatorBar : MonoBehaviour
{

    [SerializeField] Slider slider;

    private const float updateTimeSeconds = 0.4f;

    public void SetSliderMax(int maxVal)
    {
        slider.maxValue = maxVal;
    }

    public void SetSlider(int value)
    {
        StartCoroutine(GradualizeValueChange(value));
    }

    private IEnumerator GradualizeValueChange(int value)
    {
        float originalValue = slider.value;
        float elapsed = 0f;

        while (elapsed < updateTimeSeconds)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Lerp(originalValue, value, elapsed / updateTimeSeconds);
            yield return null;
        }
        slider.value = value;
        yield break;
    }
}