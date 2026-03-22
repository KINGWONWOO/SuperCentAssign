using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 화면 왼쪽 하단 사운드 On/Off 토글 버튼.
// soundOnSprite / soundOffSprite 를 Inspector에서 연결.
// 클릭 시 AudioListener 음소거 + 버튼 이미지 전환 + 스케일 애니메이션.
public class SoundToggleButton : MonoBehaviour
{
    [SerializeField] private Image   buttonImage;
    [SerializeField] private Sprite  soundOnSprite;
    [SerializeField] private Sprite  soundOffSprite;
    [SerializeField] private AudioSource clickSound;

    private bool isSoundOn = true;

    void Start()
    {
        RefreshSprite();
    }

    // UnityEngine.UI.Button의 OnClick에 연결
    public void OnClick()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        RefreshSprite();

        if (clickSound != null && isSoundOn)
            clickSound.Play();

        StopAllCoroutines();
        StartCoroutine(PunchScale());
    }

    private void RefreshSprite()
    {
        if (buttonImage == null) return;
        buttonImage.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
    }

    private IEnumerator PunchScale()
    {
        Vector3 original = transform.localScale;
        Vector3 big      = original * 1.25f;

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(original, big, t / 0.12f);
            yield return null;
        }
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(big, original, t / 0.12f);
            yield return null;
        }
        transform.localScale = original;
    }
}
