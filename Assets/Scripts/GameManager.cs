using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // TextMeshPro 사용 시 (Unity UI Text 사용 시 해당 부분 제거/수정)

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public UIManager uiManager; // Inspector에서 UIManager 할당 필요

    // UI에서 사용할 상태 표시용 텍스트 (Inspector에서 할당 필요)
    public TMP_Text statusText;

    public enum GameState { MainMenu, CreatingPoll, WaitingForPlayer, Voting, Results }
    public GameState currentState;

    // 투표 관련 데이터
    public PollArea itemA_PollArea;
    public PollArea itemB_PollArea;
    private string pollItem1_A;
    private string pollItem2_B;
    public float voteDuration = 30f; // 기본 투표 시간 (초)
    private string lastSubmittedVote; // 마지막으로 제출된 투표 (마스터 클라이언트용)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지 (필요에 따라)
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true; // 마스터 클라이언트의 씬 전환에 다른 클라이언트 자동 동기화
    }

    void Start()
    {
        ConnectToPhoton();
        // 초기 상태는 UIManager가 설정하도록 하거나, 여기서 명시적으로 호출
        // 예: uiManager?.UpdateUIForState(GameState.MainMenu); (UIManager가 null이 아닐 때 호출)
        // Start에서는 UIManager가 아직 초기화되지 않았을 수 있으므로 OnConnectedToMaster 이후에 UI 상태 변경 권장
    }

    void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("서버에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        // 상태에 따른 UI 변경은 UIManager에게 위임
        uiManager?.UpdateUIForState(newState, pollItem1_A, pollItem2_B); // UIManager가 null이 아닐 때 호출
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Debug.Log($"Status Update: {message}");
        }
    }

    #region Public Methods Called by UI or Other Scripts

    public void CreatePollRoom(string itemA, string itemB)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("서버에 아직 연결되지 않았습니다. 잠시 후 시도해주세요.");
            return;
        }

        pollItem1_A = itemA;
        pollItem2_B = itemB;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2; // 1대1 매칭
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "itemA", itemA },
            { "itemB", itemB },
            { "status", "waiting" } // "waiting", "playing" 등의 방 상태
        };
        // 로비에 공개할 프로퍼티 목록 (JoinRandomRoom 필터링에 사용)
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "itemA", "itemB", "status" };

        PhotonNetwork.CreateRoom(null, roomOptions); // 방 이름 null이면 Photon이 자동 생성
        UpdateGameState(GameState.WaitingForPlayer);
        UpdateStatus("투표 방 생성 중...");
    }

    public void JoinPollRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("서버에 아직 연결되지 않았습니다. 잠시 후 시도해주세요.");
            return;
        }
        // "waiting" 상태인 방을 찾도록 필터링
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "status", "waiting" }
        };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 2); // 최대 2명인 방
        UpdateGameState(GameState.WaitingForPlayer);
        UpdateStatus("투표 방 검색 중...");
    }

    public void LeaveRoomAndGoToMainMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom(); // 방에서 나감 (OnLeftRoom 콜백 호출됨)
        }
        else
        {
            // 방에 있지 않다면 바로 메인 메뉴 상태로 (예: 로비에서 나왔거나, 연결 실패 후)
            UpdateGameState(GameState.MainMenu);
        }
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateStatus("서버에 연결되었습니다!");
        // 로비에 접속하여 JoinRandomRoom이 더 잘 동작하도록 할 수 있음 (선택 사항)
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("로비에 접속했습니다. 투표를 만들거나 참여할 수 있습니다.");
        // 현재 게임 상태가 초기 상태이거나 방에서 나온 상태일 때만 메인 메뉴 UI 표시
        if (currentState != GameState.MainMenu && !PhotonNetwork.InRoom)
        {
            UpdateGameState(GameState.MainMenu);
        } else if (!PhotonNetwork.InRoom) { // 방에 있지 않다면 메인메뉴 상태로
            UpdateGameState(GameState.MainMenu);
        }
    }

    public override void OnCreatedRoom()
    {
        UpdateStatus($"투표 방 '{PhotonNetwork.CurrentRoom.Name}' 생성 완료. 상대방을 기다립니다...");
        // UI는 WaitingForPlayer 상태로 GameManager.CreatePollRoom에서 이미 변경됨
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"투표 방 생성 실패: {message}");
        UpdateGameState(GameState.MainMenu); // 실패 시 메인 메뉴로
    }

    public override void OnJoinedRoom()
    {
        UpdateStatus($"방 '{PhotonNetwork.CurrentRoom.Name}'에 참가했습니다. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.IsMasterClient) // 방장 (투표 생성자)
        {
            // 방장은 로컬 변수에 이미 pollItem1_A, pollItem2_B를 가지고 있음
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                // 생성하자마자 상대가 바로 들어온 경우 (또는 방장이 재접속 등)
                StartVotingPhaseIfReady();
            }
        }
        else // 참가자 (투표자)
        {
            // 방의 커스텀 프로퍼티에서 투표 항목 읽기
            pollItem1_A = (string)PhotonNetwork.CurrentRoom.CustomProperties["itemA"];
            pollItem2_B = (string)PhotonNetwork.CurrentRoom.CustomProperties["itemB"];
            // 게임 시작은 마스터 클라이언트의 RPC를 통해 이루어짐
            // UIManager를 통해 UI 업데이트 (예: "상대방을 기다리는 중" 또는 "투표 내용 로딩 완료")
            uiManager?.UpdateUIForState(currentState, pollItem1_A, pollItem2_B); // 로드된 항목으로 UI 갱신
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"방 참가 실패: {message}");
        UpdateGameState(GameState.MainMenu);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateStatus($"참여 가능한 투표 방을 찾지 못했습니다: {message}. 직접 만들어보세요!");
        UpdateGameState(GameState.MainMenu);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) // 다른 플레이어가 방에 들어왔을 때 호출됨
    {
        UpdateStatus($"상대방이 입장했습니다. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            // 방장이 볼 때, 상대방이 들어와서 2명이 되면 게임 시작
            StartVotingPhaseIfReady();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) // 다른 플레이어가 방에서 나갔을 때 호출됨
    {
        UpdateStatus("상대방이 나갔습니다.");
        if (currentState == GameState.Voting || currentState == GameState.Results || currentState == GameState.WaitingForPlayer)
        {
            // 게임이 진행 중이거나 대기 중이었다면 게임 종료 처리
            photonView.RPC("Rpc_HandleOpponentLeft", RpcTarget.All);
        }
    }

    public override void OnLeftRoom() // 자신이 방을 나갔을 때 호출됨 (LeaveRoom() 호출 결과)
    {
        UpdateStatus("방에서 나왔습니다.");
        // 투표 항목 데이터 초기화
        pollItem1_A = null;
        pollItem2_B = null;
        UpdateGameState(GameState.MainMenu);
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnected)
        {
             PhotonNetwork.JoinLobby(); // 메인 메뉴로 돌아왔으니 다시 로비 접속
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"서버 연결 끊김: {cause}. 재연결을 시도해주세요.");
        // 필요하다면 자동 재접속 로직 추가
        UpdateGameState(GameState.MainMenu); // UI를 초기 상태로
    }

    #endregion

    void StartVotingPhaseIfReady()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount != 2) return;

        // 방 상태를 "playing"으로 변경 (다른 사람이 JoinRandomRoom 시 필터링되도록)
        ExitGames.Client.Photon.Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        roomProps["status"] = "playing";
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        // 모든 클라이언트에게 투표 시작 RPC 호출
        // pollItem1_A, pollItem2_B는 마스터 클라이언트의 로컬 변수 사용
        photonView.RPC("Rpc_StartVoting", RpcTarget.All, voteDuration, pollItem1_A, pollItem2_B);
    }

    [PunRPC]
    void Rpc_StartVoting(float duration, string itemA, string itemB, PhotonMessageInfo info)
    {
        // 마스터가 보낸 정보로 로컬 변수 확실히 업데이트 (특히 투표자)
        pollItem1_A = itemA;
        pollItem2_B = itemB;
        voteDuration = duration;
        lastSubmittedVote = null; // 이전 투표 결과 초기화

        if (itemA_PollArea != null)
        {
            itemA_PollArea.itemName = itemA;
        }
        else
        {
            Debug.LogError("GameManager: itemA_PollArea not assigned!");
        }

        if (itemB_PollArea != null)
        {
            itemB_PollArea.itemName = itemB;
        }
        else
        {
            Debug.LogError("GameManager: itemB_PollArea not assigned!");
        }

        UpdateGameState(GameState.Voting);
        UpdateStatus($"투표 시작! '{itemA}' 또는 '{itemB}'를 선택하세요. 시간: {duration}초");

        // 투표자는 캐릭터 스폰 (GameManager가 직접 스폰하거나, VoterCharacter 관련 로직 호출)
        if (!PhotonNetwork.IsMasterClient)
        {
            // 예시: 캐릭터 스폰 위치는 미리 정의된 곳 사용
            Vector3 spawnPosition = new Vector3(0, 3, 0); // 실제 스폰 위치로 변경
            PhotonNetwork.Instantiate("Prefabs/VoterCharacter", spawnPosition, Quaternion.identity);
            // UIManager의 타이머 UI 시작
            uiManager?.StartVoteTimerUI(duration);
        }
        else // 마스터 클라이언트는 투표 종료 타이머 시작
        {
            CancelInvoke("EndVotingPhaseByTime"); // 이전 Invoke가 있다면 취소
            Invoke("EndVotingPhaseByTime", duration);
             // 마스터 클라이언트도 자신의 타이머 UI 시작
            uiManager?.StartVoteTimerUI(duration);
        }
    }

    // VoterCharacter에서 이 RPC를 호출하여 투표 제출
    [PunRPC]
    public void Rpc_SubmitVote(string votedItem, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터 클라이언트만 이 RPC를 처리

        if (currentState != GameState.Voting) return; // 이미 투표가 끝났다면 무시

        //Player voter = info.Sender; // 투표한 플레이어 (여기서는 상대방)
        UpdateStatus($"상대방이 '{votedItem}'에 투표했습니다.");
        lastSubmittedVote = votedItem; // 마스터 클라이언트가 투표 결과 저장

        CancelInvoke("EndVotingPhaseByTime"); // 타이머가 끝나기 전에 투표했으므로 기존 Invoke 취소
        AnnounceResult(lastSubmittedVote); // 즉시 결과 발표
    }

    void EndVotingPhaseByTime() // 마스터 클라이언트에서만 호출됨
    {
        if (!PhotonNetwork.IsMasterClient || currentState != GameState.Voting) return;

        UpdateStatus("시간 초과!");
        // 시간 초과 시, lastSubmittedVote가 null (즉, 상대방이 투표 안 함)이면 무효표 처리
        AnnounceResult(string.IsNullOrEmpty(lastSubmittedVote) ? "시간 초과 (투표 없음)" : lastSubmittedVote);
    }

    void AnnounceResult(string result) // 마스터 클라이언트에서만 호출됨
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("Rpc_AnnounceResults", RpcTarget.All, result);
    }

    [PunRPC]
    void Rpc_AnnounceResults(string finalResult)
    {
        UpdateGameState(GameState.Results);
        UpdateStatus($"투표 결과: {finalResult}");
        uiManager?.ShowResults(finalResult); // UIManager를 통해 결과 표시

        // N초 후 자동으로 메인 메뉴로 돌아가거나 "Play Again" 옵션 제공
        if (PhotonNetwork.IsMasterClient)
        {
            // 예시: 5초 후 현재 방을 나가고 메인 메뉴로 (다른 플레이어도 함께)
            Invoke("DelayedLeaveRoomAndGoToMainMenu", 5f);
        }
    }

    [PunRPC]
    void Rpc_HandleOpponentLeft()
    {
        UpdateStatus("상대방이 나갔습니다. 게임이 종료됩니다.");
        // 현재 상태에 따라 UI 정리
        if (currentState == GameState.Voting || currentState == GameState.Results)
        {
            uiManager?.HideAllGamePanels(); // 예시: 모든 게임 패널 숨기기
            uiManager?.ShowPopupMessage("상대방이 나갔습니다. 메인 메뉴로 돌아갑니다.");
        }
        
        // 마스터 클라이언트만 방을 나가는 로직을 트리거하거나, 모든 클라이언트가 각자 나감
        // 현재는 모든 클라이언트가 이 RPC를 받으므로, 각자 나가는 로직 수행 가능
        if (PhotonNetwork.InRoom)
        {
            // 약간의 딜레이 후 방 나가기 (메시지 볼 시간)
            Invoke("DelayedLeaveRoomAndGoToMainMenu", 3f);
        }
    }

    void DelayedLeaveRoomAndGoToMainMenu()
    {
        // 방에 남아있는 플레이어가 마스터라면 방을 닫고 나갈 수 있음
        // if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        // {
        //     PhotonNetwork.CurrentRoom.IsOpen = false;
        //     PhotonNetwork.CurrentRoom.IsVisible = false;
        // }
        LeaveRoomAndGoToMainMenu();
    }
}