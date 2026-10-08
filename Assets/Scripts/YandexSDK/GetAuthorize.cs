using UnityEngine;
using YG;
using TMPro;

public class GetAuthorize : MonoBehaviour
{
    [SerializeField] TMP_Text status;

    private void OnEnable()
    {
        YG2.onGetSDKData += OnAuth;
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= OnAuth;
    }

    void Start()
    {
        UpdateStatus();
    }

    // Yandex Games forbids opening the auth dialog on startup: hook this to a login button.
    public void OnLoginClicked()
    {
        if (!YG2.player.auth)
        {
            YG2.OpenAuthDialog();
        }
    }

    private void OnAuth()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (status == null) return;

        if (YG2.player.auth)
        {
            status.text = YG2.player.name + "\n" + YG2.player.id;
        }
        else
        {
            status.text = "Не авторизован";
        }
    }
}