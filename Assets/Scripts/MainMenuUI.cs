using UnityEngine;
using UnityEngine.UI; // 기본 UI 버튼 사용 시

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")] public Button createPollButton;
    public Button joinPollButton;

    void Start()
    {
        // 버튼이 null이 아닌지 확인 후 리스너 추가
        if (createPollButton != null)
            createPollButton.onClick.AddListener(OnCreatePollClicked);
        else
            Debug.LogError("Create Poll Button is not assigned in MainMenuUI.");

        if (joinPollButton != null)
            joinPollButton.onClick.AddListener(OnJoinPollClicked);
        else
            Debug.LogError("Join Poll Button is not assigned in MainMenuUI.");
    }

    void OnCreatePollClicked()
    {
        // GameManager의 상태를 변경하여 UIManager가 PollCreationPanel을 표시하도록 함
        GameManager.Instance.UpdateGameState(GameManager.GameState.CreatingPoll);
    }

    void OnJoinPollClicked()
    {
        GameManager.Instance.JoinPollRoom(); // GameManager를 통해 방 참가 시도
    }
}