using UnityEngine;
using UnityEngine.UI; // 기본 UI 버튼 사용 시
using TMPro; // TextMeshPro InputField 사용 시

public class PollCreationUI : MonoBehaviour
{
    [Header("InputFields")] public TMP_InputField itemAInputField;
    public TMP_InputField itemBInputField;

    [Header("Buttons")] public Button startPollButton;
    public Button backButton; // 메인 메뉴로 돌아가기 버튼

    void Start()
    {
        if (startPollButton != null)
            startPollButton.onClick.AddListener(OnStartPollClicked);
        else
            Debug.LogError("Start Poll Button is not assigned in PollCreationUI.");

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        else
            Debug.LogError("Back Button is not assigned in PollCreationUI.");
    }

    void OnStartPollClicked()
    {
        string itemA = itemAInputField.text;
        string itemB = itemBInputField.text;

        if (string.IsNullOrWhiteSpace(itemA) || string.IsNullOrWhiteSpace(itemB))
        {
            // UIManager를 통해 사용자에게 알림을 표시하거나, GameManager의 상태 텍스트 업데이트
            GameManager.Instance.UpdateStatus("두 가지 투표 항목을 모두 입력해주세요.");
            UIManager.Instance.ShowPopupMessage("두 가지 투표 항목을 모두 입력해주세요."); // 팝업 메시지 사용 예
            return;
        }

        GameManager.Instance.CreatePollRoom(itemA, itemB); // GameManager를 통해 방 생성 요청
    }

    void OnBackClicked()
    {
        // GameManager의 상태를 변경하여 UIManager가 MainMenuPanel을 표시하도록 함
        GameManager.Instance.UpdateGameState(GameManager.GameState.MainMenu);
    }
}