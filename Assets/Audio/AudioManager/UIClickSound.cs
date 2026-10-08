using UnityEngine;

public class UIClickSound : MonoBehaviour
{
    public void Click()
    {
        UIAudio.Instance.Click();
    }
}