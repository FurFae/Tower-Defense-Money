using UnityEngine;

public class UIAudio : MonoBehaviour
{
    public static UIAudio Instance;

    [SerializeField] private AudioSource source;

    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip hover;

    private void Awake()
    {
        Instance = this;
    }

    public void Click()
    {
        source.PlayOneShot(click);
    }

    public void Hover()
    {
        source.PlayOneShot(hover);
    }
}