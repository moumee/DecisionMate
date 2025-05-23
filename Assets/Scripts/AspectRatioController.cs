using UnityEngine;

public class AspectRatioController : MonoBehaviour
{
    // 원하는 세로 비율 (예: 9:16)
    public float targetAspectWidth = 9.0f;
    public float targetAspectHeight = 16.0f;

    void Start()
    {
        SetViewport();
    }

    void Update() // 화면 크기가 변경될 수 있으므로 Update에서도 호출 (선택 사항)
    {
        SetViewport();
    }

    void SetViewport()
    {
        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        if (scaleHeight < 1.0f) // 화면이 타겟 비율보다 가로로 넓은 경우 (레터박스)
        {
            Rect rect = camera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            camera.rect = rect;
        }
        else // 화면이 타겟 비율보다 세로로 긴 경우 (필러박스) - 모바일 세로 게임이므로 이 경우가 주로 해당됨
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = camera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            camera.rect = rect;
        }
    }
}