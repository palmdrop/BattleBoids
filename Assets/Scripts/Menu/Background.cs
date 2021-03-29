using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Background : MonoBehaviour
{
    [SerializeField] Canvas canvas;

    void Start() {
        ScaleToFullScreen();
    }

    void Update() {
        ScaleToFullScreen();
    }

    public void ScaleToFullScreen() {
        Vector2 spriteSize = GetComponent<Image>().sprite.bounds.size;
        float spriteRatio = spriteSize.x / spriteSize.y;
        float screenRatio = Camera.main.aspect;

        float width;
        float height;

        if (spriteRatio >= screenRatio) {
            // scale height to screen height
            height = Screen.height;
            width = height * spriteRatio;
        } else {
            // scale width to screen width
            width = Screen.width;
            height = width / spriteRatio;
        }

        Vector2 size = new Vector2(width, height) / canvas.scaleFactor;
        GetComponent<RectTransform>().sizeDelta = size;
    }
}
