using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//네임스페이스로 UI타입가져옴
using UnityEngine.SceneManagement;//네임스페이스로 장면 가져옴. 다시 시작할 때 사용


public class GameManager : MonoBehaviour
{
    //[Header]로 인스펙터의 말머리를 추가
    [Header("핵심변수")]
    public int maxLevel;
    public int score; //점수담당 변수
    public bool isOver; //게임오버 상태 저장할 변수 추가

    [Header("오브젝트 풀링")]
    public Dongle lastDongle; //동글 타입 변수 선언후  초기화
    public GameObject donglePrefab;//프리펩 변수 선언 및 초기화
    public Transform dongleGroup; //동글 그룹 오브젝트 선언 및 초기화

    public List<Dongle> donglePool;//오브젝트 풀을 담당할 리스트 변수 선언

    public GameObject effectPrefab;
    public Transform effectGroup; //파이클 이펙트 그룹 오브젝트 선언 및 초기화

    public List<ParticleSystem> effectPool;

    [Range(1, 30)] //인스펙터상 조절 해야하는걸 더 간편하게하는 속성 키워드, 최소값과 최대값을 슬라이더로 설정할 수 있게해줌.
    public int poolSize; //오브젝트 풀 사이즈 지정 관리용(얼마나 오브젝트를 생성할 것인가)
    public int poolCursor; //어느 부분을 참고하고 있는가를 관리하는 용도

    [Header("오디오")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer; //배열  형식으로 오디오소스값 가져옴
    public AudioClip[] sfxClip; // 효과음들을 담을 변수
    public enum Sfx {LevelUp, Next, Attach, Button, Over};//문자열말고 enum이라는걸 이용해 지정, enum:상수들의 집합과도 같은 열거형타입. -> 장면 빌드 순서 가져온 것처럼 사용할 수 있게 해준다.
    int sfxCursor;//오디오 소스를 가리키는 변수 선언

    [Header("UI")]
    public GameObject startGroup; //게임 시작 그룹 오브젝트를 저장할 변수 선언  및 초기화
    public GameObject endGroup; //게임 종료 그룹 오브젝트를 저장할 변수 선언 및 초기화
    public Text scoreText; //점수표기 텍스트를 담을 변수선언
    public Text maxScoreText; //역대 최고점수 표기를 위해 텍스트 컴포넌트 가져옴
    public Text subScoreText; //게임종료 후 점수 표기용

    [Header("기타")]
    public GameObject line; //활성화 용으로 가져옴 bottom도 마찬가지
    public GameObject bottom;   

    void Awake(){
        //프레임 안정화, targetFrameRate -> 프레임 설정 속성
        Application.targetFrameRate = 60;

        //오브젝트 풀 호출은 여기서 만든걸
        donglePool = new List<Dongle>(); //awake에서 오브젝트 풀 리스트 초기화
        effectPool = new List<ParticleSystem>();

        for(int index = 0; index < poolSize; index++){ //폴문과 풀 사이즈를 이용해여 풀 생성함수를 여러번 호출, 오브젝트 풀은 은행처럼 오브젝트 저장을 미리해서 나중에 계속 한개씩 불러서 메모리 손해가 나는 걸 방지
            MakeDongle();
        }

        //최고점수는 미리 불러와야하니까 awake생명주기 사용
        if(!PlayerPrefs.HasKey("MaxScore")) {//has key -> 저장된 데이터가 있는지 확인하는 함수, 처음 시작할 때 오류안나게(값이 없어서) 
            PlayerPrefs.SetInt("MaxScore", 0); //getint는 불러오기, setint는 저장
        } 

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString(); //데이터 저장을 담당하는 클래스, float,int, string 3가지 형태를 데이터 저장가능

    }
    public void GameStart() //게임 시작 버튼 누르면 시작
    {
        //오브젝트 활성화
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);

        //시작화면 오브젝트 비활성화
        startGroup.SetActive(false);


        //사운드 시작
        bgmPlayer.Play(); //PLAY 오디오소스의 오디오 클립을 재생하는 함수
        SfxPlay(Sfx.Button);

        // 게임 시작(동글 생성)
        Invoke("NextDongle", 1.5f); // 함수호출에 딜레이를 주고 싶을 때 사용하는 함수 -> 1.5초 뒤 함수 호출
    }

    Dongle MakeDongle() //오브젝트 풀을 만들기 위한 함수 생성
    {
        //이펙트 생성
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect "+ effectPool.Count; //아이디개념같은걸로 넣음, -> 카운트

        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();  //위 코드를 이용해 파티클 시스템도 설정
        effectPool.Add(instantEffect); //list.add 해당리스트에 데이터를 추가하는 함수     

        //동글 생성
        //게임 오브젝트라 리턴이 안돼서, 초기화 해주는 리턴을 따로빼줌
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle "+ donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        //Instantiate -> 오브젝트를 새로 생성해주는 함수
       
        //동글 생성하면서 manager, effect 변수를 같이 초기화
        instantDongle.manager = this; //변수 초기화, 내 자신의 스크립트를 그대로 넣음. (자기 주소)
        instantDongle.effect = instantEffect; //동글 생성하면서 바로 이벤트 변수를 생성했던 것으로 초기화
        donglePool.Add(instantDongle); //list.add 해당리스트에 데이터를 추가하는 함수 
        return instantDongle;
    }

    Dongle GetDongle() //함수 반환타입을 동글로 선언, 함수를 변수로 만든 것
    {
        //풀 활용, 오브젝트 풀 탐색할 때 폴문사용
        for(int index=0; index<donglePool.Count; index++) {
            poolCursor = (poolCursor + 1) % donglePool.Count; //이것도 풀 사이즈 설정 값을, 넘길 수도있으니까 나머지연산으로 안 넘게(효율 높게)
            if(!donglePool[poolCursor].gameObject.activeSelf){ //혹시 그 커서위치(번지)의 오브젝트가 활성화가 되었나요의 !(반대)
                return donglePool[poolCursor]; //커서지목했는데 대기중이면 리턴으로 반환
            }
        }

        return MakeDongle(); //원래 값을 넣어야 됐는데(함수말고) 메이크 함수도 반환값으로 자신이 만든 것을 리턴해서 리턴으로 사용가능 + 다시 오브젝트 생성 할 수 있음.
        //근데 부족하지 않게 오브젝트 풀링 재사용을 해줘서 상관없게 해놔야함
    }

    //새 동글 가지고 오는 로직(다음 동글, 동글 생성함수 따로)
    void NextDongle()
    {


        if(isOver){
            return;//게임오버하면 공 안나오게
        }

        lastDongle = GetDongle(); //변수화 시켰으니까 쉽게 사용가능 + 오브젝트 풀링 결과물
        
        lastDongle.level = Random.Range(0, maxLevel); //지금 존재하는것만큼 맥스값 늘려줄라고 만듬만큼 나오는 거 늘음
        lastDongle.gameObject.SetActive(true); //setactive 오브젝트 활성화 함수
        //랜덤으로 나오는 동글을 정해주고 -> 오브젝트 활성화. + 그래서 미리 프리펩 비활성화 설정해놓음
        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext");
    }

    //코루틴 -> 로직 제어를 유니티에 다 맡김. 여기서는 넥스트 동글을 연타로 쓸 때, 유니티 안 멈추게
    IEnumerator WaitNext()
    {
        while (lastDongle != null)
        {
            yield return null;
        }
        //yield 유니티가 코루틴을  제어하기 위한 키워드 null은 한프레임 WaitForSeconds(2.5f);은 시간으로 설정가능하게 해줌.
        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }

    //이벤트 트리거 역할을 대신해줄 (게임매니저가)
    public void TouchDown(){
        //보이드도 리턴가능. 단, 뒤에 값없이
        if(lastDongle == null)
            return;

        lastDongle.Drag();
    }
    public void TouchUp(){
        if(lastDongle == null)
            return;      

        lastDongle.Drop(); // 드랍하고, 보관용으로 저장해둔 변수는 null로 값 비우기
        lastDongle = null;
    }

    public void GameOver()
    {//여기서 함수선언하고 동글에서 호출
        if (isOver){
            return; //함수 실행 한번만 하도록
        }

        isOver = true;

        StartCoroutine("GameOverRoutine");

    }

    IEnumerator GameOverRoutine()
    {
        //게임오버되면 남은 공들 다지우고 점수환산

        //1.장면 안에 활성화 되어있는 모든 동글 가져오기
        Dongle[] dongles = FindObjectsOfType<Dongle>();//장면에 올라온 <>안의 컴포넌트들을 탐색 한후 배열 안에 저장 

        //2.지우기전에 모든 동글의 물리효과 비활성화 의도치 않은 합체 방지
        for(int index=0; index < dongles.Length; index++){ //0부터시작하니까 순서를 맞출려고
            dongles[index].rigid.simulated = false; //시뮬레이터 됨 설정 해제          
        }
        //3. 1번의 목록을 하나씩 접근해서 지우기
        for(int index=0; index < dongles.Length; index++){ //0부터시작하니까 순서맞출려고
            dongles[index].Hide(Vector3.up * 100); //그냥 게임플레이중에는 나올 수 없는큰값을 줘서 숨기기 본래위치에서 날려보내는것
            yield return new WaitForSeconds(0.1f); //사라지기 전 시간 조정. (한 번에 다x)
        }

        yield return new WaitForSeconds(1f); //게임오버 코루틴에서 모든 결산이 끝난후 배치

        //게임오버 삭제점수까지 있어야하니까 게임오버 코루틴에서
        //최고 점수  코루틴
        int  maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore")); //저장된 값과 현재스코어중 높은걸 맥스 스코어값으로 가져오겠다.
        PlayerPrefs.SetInt("MaxScore", maxScore); //위의 저장된값을 보내주기

        // 게임오버 UI표시
        subScoreText.text = "점수 : " + scoreText.text; //점수 그대로 버튼 텍스트에 붙이기
        endGroup.SetActive(true);//활성화

        bgmPlayer.Stop(); //종료 음악나오기 전에 배경음악 정지
        SfxPlay(Sfx.Over);
    }

    public void Reset() //게임 재시작을 위한 함수
    {
        SfxPlay(Sfx.Button);
        StartCoroutine("ResetCorotin");
    }

    //효과음 듣고나서 딜레이가 있게 코루틴 사용
    IEnumerator ResetCorotin()
    {
        yield return new WaitForSeconds(1f);

        //처음 장면을 불러오자
        SceneManager.LoadScene(0); //하나있는 씬인 0번씬 불러오기
    }

    public void SfxPlay(Sfx type) //효과음 재생시켜주는 함수 선언 클립받아 인수
    {
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)]; //레벨업 사운드 랜덤 출력 클립에 있는거
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3]; //sfxclip배열에 들어있는 해당 번호에 할당된 오디오 가져옴.
                break;  
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4]; 
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5]; 
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6]; 
                break;      //커서는 클립의 순서를 플레이어에 가르쳐주는 것.                                                    
        }

        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length; //처음 커서 값이 3이니까 커서값을 0,1,2 계속돌릴 수 있게 수식 추가
    }

    void Update() {
        //모바일에서 나가는 기능 추가
        if(Input.GetButtonDown("Cancel")){ //모바일에서 뒤로가기 키
            Application.Quit(); //게임 나가기
        }
    }

    void LateUpdate() { //업데이트 종료 후 실행되는 생명주기 함수(업데이트 가지고 활용해야하기 때문에 이 생명주기 사용)
        scoreText.text = score.ToString(); //스코어 텍스트에 스코어 값 넣어줌. (스코어가 인트형이라 문자형으로 바꿔줌)
    }
}
