using UnityEngine;

public class PollArea : MonoBehaviour
{
    // Inspector에서 각 투표 영역에 해당하는 아이템 이름을 설정합니다.
    // 예: "사과", "바나나" (GameManager에서 사용한 pollItem1_A, pollItem2_B 문자열과 일치해야 함)
    public string itemName;

    void Awake()
    {
        // 이 GameObject에는 반드시 Collider가 있어야 하고, IsTrigger = true로 설정되어야 함
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"PollArea '{gameObject.name}' is missing a Collider component.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning(
                $"Collider on PollArea '{gameObject.name}' is not set to IsTrigger. Consider setting it if using OnTrigger methods.");
        }
    }
}