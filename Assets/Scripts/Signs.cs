// Signs.cs

using UnityEngine;

public class Signs : MonoBehaviour
{
    [SerializeField] private SpriteRenderer itemA_SpriteRenderer;
    [SerializeField] private SpriteRenderer itemB_SpriteRenderer;

    [SerializeField] private Sprite normal_Sprite;
    [SerializeField] private Sprite selected_Sprite;

    // Update 함수는 GameManager가 RPC를 통해 직접 업데이트하므로 제거합니다.
    /*
    private void Update()
    {
        if (VoterCharacter.LocalInstance)
        {
            if (VoterCharacter.LocalInstance.gameObject.transform.position.x < 0)
            {
                itemA_SpriteRenderer.sprite = selected_Sprite;
                itemB_SpriteRenderer.sprite = normal_Sprite;
            }
            else
            {
                itemA_SpriteRenderer.sprite = normal_Sprite;
                itemB_SpriteRenderer.sprite = selected_Sprite;
            }
        }
    }
    */

    // GameManager가 RPC를 통해 호출할 함수
    public void UpdateSignDisplay(int selectedItemIndex) // 0은 A항목, 1은 B항목 의미
    {
        if (itemA_SpriteRenderer == null || itemB_SpriteRenderer == null || normal_Sprite == null ||
            selected_Sprite == null)
        {
            Debug.LogWarning("[Signs] 스프라이트 또는 렌더러가 할당되지 않았습니다.");
            return;
        }

        if (selectedItemIndex == 0) // A 항목 선택됨
        {
            itemA_SpriteRenderer.sprite = selected_Sprite;
            itemB_SpriteRenderer.sprite = normal_Sprite;
        }
        else if (selectedItemIndex == 1) // B 항목 선택됨
        {
            itemA_SpriteRenderer.sprite = normal_Sprite;
            itemB_SpriteRenderer.sprite = selected_Sprite;
        }
        else // 선택되지 않음 (예: 초기 상태 또는 중앙) - 필요에 따라 이 로직 추가/수정
        {
            itemA_SpriteRenderer.sprite = normal_Sprite;
            itemB_SpriteRenderer.sprite = normal_Sprite;
        }
    }
}