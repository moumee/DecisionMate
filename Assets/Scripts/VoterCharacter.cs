using UnityEngine;
using Photon.Pun;

public class VoterCharacter : MonoBehaviourPunCallbacks
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb2D; // 3D Rigidbody 대신 Rigidbody2D 사용
    private SpriteRenderer spriteRenderer; // 스프라이트 뒤집기를 위해 필요

    
    private string currentPollAreaItemName = "";

    private float localVoteTimer = 0f;
    private bool isLocalTimerRunning = false;

    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!photonView.IsMine) // 자신이 생성한 캐릭터가 아니면 비활성화
        {
            enabled = false; // 이 스크립트의 Update, FixedUpdate 등을 비활성화
            if (rb2D != null)
            {
                // 다른 플레이어의 캐릭터는 물리적으로 시뮬레이션되지만 직접 조작은 안 되도록 isKinematic으로 설정
                // 또는 bodyType을 Kinematic으로 변경 (상황에 따라 Static으로 할 수도 있음)
                rb2D.bodyType = RigidbodyType2D.Kinematic;
            }
            return;
        }
    }

    // GameManager의 Rpc_StartVoting에서 VoterCharacter의 이 함수를 호출하여 타이머를 시작하도록 설계
    public void InitializeVoting(float duration)
    {
        if (!photonView.IsMine) return;

        localVoteTimer = duration;
        isLocalTimerRunning = true;
        enabled = true; // 혹시 비활성화 되어있었다면 다시 활성화
        if (rb2D != null) rb2D.bodyType = RigidbodyType2D.Dynamic; // 조작 가능하도록
    }

    void Update()
    {
        if (!photonView.IsMine || !enabled) return; // 자신이 아니거나, 비활성화 상태면 실행 안 함

        // 타이머 로직
        if (isLocalTimerRunning)
        {
            localVoteTimer -= Time.deltaTime;
            if (localVoteTimer <= 0)
            {
                isLocalTimerRunning = false;
                FinalizeVote(); // 로컬 타이머 종료 시 투표 제출
            }
        }
    }

    void FixedUpdate() // 물리 기반 이동은 FixedUpdate에서 처리하는 것이 좋음
    {
        if (!photonView.IsMine || !enabled) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        // Project Settings > Input Manager에 정의된 기본 축 이름 사용
        float moveHorizontal = Input.GetAxis("Horizontal"); // 보통 A/D 또는 좌우 화살표
        

        // Rigidbody2D를 사용한 이동 (물리적 충돌 반응)
        if (rb2D != null)
        {
            // 옵션 1: 속도 직접 제어 (약간 더 반응성이 좋지만, 물리적 관성이 덜 느껴질 수 있음)
            // rb2D.velocity = movement * moveSpeed;

            // 옵션 2: MovePosition 사용 (물리적 충돌을 존중하며 부드럽게 이동, Interpolation 설정과 잘 맞음)
            rb2D.MovePosition(rb2D.position + new Vector2(moveHorizontal * moveSpeed * Time.fixedDeltaTime, 0));
        }
        else // Rigidbody2D가 없다면 Transform으로 이동 (물리 충돌 무시)
        {
            transform.Translate(new Vector2(moveHorizontal * moveSpeed * Time.deltaTime, 0), Space.World);
        }

        // 스프라이트 방향 전환 (좌우 뒤집기)
        if (spriteRenderer != null)
        {
            if (moveHorizontal > 0.01f) // 오른쪽으로 이동 중
            {
                spriteRenderer.flipX = true; // 기본 방향 (오른쪽을 보도록 설정된 스프라이트 기준)
            }
            else if (moveHorizontal < -0.01f) // 왼쪽으로 이동 중
            {
                spriteRenderer.flipX = false; // 왼쪽으로 뒤집기
            }
            // moveHorizontal이 0에 가까우면 마지막 방향 유지
        }
    }

    // 투표 영역과의 충돌/트리거 감지 (2D Collider 사용)
    void OnTriggerStay2D(Collider2D other) // 2D에서는 Collider2D, OnTriggerStay2D 사용
    {
        if (!photonView.IsMine) return;

        PollArea pollArea = other.GetComponent<PollArea>(); // PollArea 스크립트는 그대로 사용 가능
        if (pollArea != null)
        {
            currentPollAreaItemName = pollArea.itemName;
        }
    }

    // void OnTriggerExit2D(Collider2D other) // 2D에서는 Collider2D, OnTriggerExit2D 사용
    // {
    //     if (!photonView.IsMine) return;
    //
    //     PollArea pollArea = other.GetComponent<PollArea>();
    //     if (pollArea != null && pollArea.itemName == currentPollAreaItemName)
    //     {
    //         currentPollAreaItemName = ""; // 영역에서 벗어나면 현재 영역 정보 초기화
    //     }
    // }

    public void FinalizeVote()
    {
        if (!photonView.IsMine || !enabled) return;

        string voteToSend = "";
        if (!string.IsNullOrEmpty(currentPollAreaItemName))
        {
            voteToSend = currentPollAreaItemName;
            Debug.Log($"[VoterCharacter] 투표 제출: {voteToSend}");
        }
        else
        {
            voteToSend = "선택 없음";
            Debug.Log("[VoterCharacter] 투표 제출: 선택 없음 (영역 밖)");
        }

        if (GameManager.Instance != null && GameManager.Instance.photonView != null)
        {
            GameManager.Instance.photonView.RPC("Rpc_SubmitVote", RpcTarget.MasterClient, voteToSend);
        }
        else
        {
            Debug.LogError("[VoterCharacter] GameManager 인스턴스 또는 PhotonView를 찾을 수 없습니다.");
        }

        enabled = false; // 투표 제출 후에는 더 이상 조작 및 업데이트 안 함
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero; // 움직임 정지
        // 필요하다면 PhotonNetwork.Destroy(gameObject); 등을 통해 캐릭터를 파괴할 수도 있음
    }
}