using System.Collections;
using UnityEngine;

public class FadeText : MonoBehaviour
{
    private Color32 opaqueColor;
    private Color32 transparentColor;
    private TextMesh thisText;
    private bool doFadeIn = false;
    private readonly float time = 1;
    private float timer = 0;

    void Awake()
    {
        thisText = GetComponent<TextMesh>();
        opaqueColor = thisText.color;
        transparentColor = new Color32(opaqueColor.r, opaqueColor.g, opaqueColor.b, 0);
        thisText.color = transparentColor;
    }

    public void FadeIn(string newText)
    {
        timer = 0;
        thisText.color = transparentColor;
        thisText.text = newText;
        StartCoroutine(StartFade());
    }

    private IEnumerator StartFade()
    {
        doFadeIn = true;
        yield return new WaitForSeconds(2);
        doFadeIn = false;
    }

    void Update()
    {
        if (doFadeIn)
        {
            timer += Time.deltaTime / time;
            var lerped = Color32.Lerp(thisText.color, opaqueColor, timer);
            thisText.color = lerped;
        }
    }
}
