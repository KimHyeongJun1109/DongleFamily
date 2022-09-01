using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager; //퍼블릭으로 게임매니저 가져옴
    public ParticleSystem effect; //자신만의 파티클 이펙트를 담을 변수추가
    public bool isDrag;  //드래그하는 위치 제약을 걸어주기 위한 변수 선언
    public Rigidbody2D rigid; //Rigidbody2D 컴포넌트 가져옴
    public int level; //레벨을 인트값으로 저장할 퍼블릭 변수 선언
    Animator anim; //애니메이터 컴포넌트 가져옴
    public bool isMerge;  //합체 중일 때 다른 오브젝트들이 간섭하지 않도록 관리를 도와주기위한 변수 선언
    public bool isAttach; //충돌이 동작했는지 저장하는 변수 선언
    CircleCollider2D circle;
    SpriteRenderer spriteRenderer;

    float deadTime; //경계선 걸려있는 시간 저장할 변수 추가

    void Awake() { //가져온 컴포넌트들 함수를 통해 값넣고 초기화
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        circle = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable() { //스크립트가 활성화되면 실행되는 이벤트 함수
        //SetInteger -> 애니메이터의 int형 파라메터를 설정하는 함수로 설정된 파라미터 가져오는 용도, 뒤에 선언된 변수는 위에 퍼블릭으로 선언한 것
        anim.SetInteger("Level", level);
    }

    //재사용로직, 비활성화되면 실행하는 이벤트 함수
    void OnDisable() {

        //동글 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        //동글 트랜스폼 초기화
        transform.localPosition = Vector3.zero;//그냥 포지션아닌게 생성된 애들이 다 그룹안에 있으니까 걸맞는 
        transform.localRotation = Quaternion.identity; //이것도 벡터3 젤로마냥 0값주는거
        transform.localScale = Vector3.zero;

        //동글 물리초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero; //속도 0
        rigid.angularVelocity = 0; //회전속도
        circle.enabled = true;//숨겨진애 서클 콜라이더 다시켜줌 그냥 새걸로 만드네
    
    }

    void Update()
    {
        if(isDrag){
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); //마우스 위치
            //벽 안 넘어가게 x축 일정 고정
            float leftBorder = -4.2f + transform.localScale.x / 2f; //벽과 동글 반지름 감안
            float rightBorder = 4.2f - transform.localScale.x / 2f;

            //if문으로 좌 우측 경계값 이동 제한
            if(mousePos.x < leftBorder) {
            mousePos.x = leftBorder;
            }
            else if (mousePos.x > rightBorder) // if, else if로 양쪽 벽 넘어서 안가게 설정
            {
              mousePos.x = rightBorder;
            }
            mousePos.y = 8; //높이는 고정되게
            mousePos.z = 0; //동글이 z축을 따라가지 않도록 0으로
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f); //lerp로 부드럽게, Lerp(현재위치, 목표위치, 속도)
        } 
    }

    public void Drag(){
        isDrag = true;  //Drag, Drop으로 동글이 떨어지는데 영향을 주는(마우스를 따라가는) isDrag값 설정
    }
    public void Drop(){
        isDrag = false;
        rigid.simulated = true; //시뮬레이션 됨 기능을 켜줌
    }

    void OnCollisionEnter2D(Collision2D collision) { //물리적 충돌을 하면
        StartCoroutine("AttachRoutine");
    }
    //소리가 계속나서 시끄럽지 않게 하기 위해 소리나는데에 시간 걸리게하기 (충돌음 제한)

    IEnumerator AttachRoutine()
    {
        if (isAttach){
            yield break; //소리내면 탈출하게
        }

        isAttach = true; //if문에 이용해서 값 탈출하게 도와주는 함수
        manager.SfxPlay(GameManager.Sfx.Attach); //매니저에 있는 오디오소스를 이용
        yield return new WaitForSeconds(0.2f); //설정한 시간만큼 기다린다.
        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Dongle"){ // 부딪힌게 동글이면
            Dongle other = collision.gameObject.GetComponent<Dongle>(); //동글에 부딪히면 정보를 가져오게

            if(level == other.level && !isMerge && !other.isMerge && level < 7){ //부딪힌게 레벨이 같으면 + 버그없이 하기 위해 조건 추가 설정
                //동글 합치기 로직             
                
                //두개 동글 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;
                //내가 아래에 있을 때, 동일한 높이일 때, 내가 오른쪽에 있을때
                if(meY < otherY || (meY == otherY && meX > otherX)){
                    //상대방 숨기기
                    other.Hide(transform.position);//내쪽으로 이동해서 사라져야하기때문에 자기위치로 값줌.
                    //나는 레벨업
                    LevelUp();
                }
               
            }
        }
    }
    public void Hide(Vector3 targetPos) //사라질 위치를 매개변수로 받고
    {
        isMerge = true; //합쳐지고 있으면
        rigid.simulated = false;//흡수 이동을 위해 물리효과 비활성화
        circle.enabled = false; //콜라이더의 경우 enabled 속성으로 비활성화

        //게임매니저에 가지고 가기 편하도록 여기서 effectplay 사용
        if(targetPos == Vector3.up * 100) {
            EffectPlay(); //순서상 이펙트가 나오고 위치를 이동함
        }
        StartCoroutine(HideRoutine(targetPos)); //이동하는 과정을 코루틴으로 해줌
    }

    IEnumerator HideRoutine(Vector3 targetPos){
        int frameCount = 0;

        while(frameCount < 20){ //while문으로 update생명주기처럼 이용
            frameCount++; //무한루프안하게
            if(targetPos != Vector3.up * 100) {//게임오버용으로 따로 분리
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f); //부드럽게 이동 자신위치 목표지점 속도
            }
            else if(targetPos == Vector3.up * 100) {
                //크기 부드럽게 작아지게 현재 크기 목표크기 속도
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            yield return null; //부드럽게 보이도록 1프레임 쉼
        }  
        
        manager.score += (int)Mathf.Pow(2, level); //점수 누적값에다가 레벨의 거듭제곱을 값으로(점수로) 줌. + pow가 float 형태라 오류나서 int로 강제 형변환 (지금 2라서)

        isMerge = false; //합체끝남
        gameObject.SetActive(false); //While문 끝나면 잠금 해제하면서 오브젝트 비활성화
    }

    void LevelUp()
    {
        isMerge = true; //이것도 합체처럼

        rigid.velocity = Vector2.zero;//레벨업할 때 방해가 될수 있는 물리속도 제거 -> 부딪힘으로 안 튕겨나가게
        rigid.angularVelocity = 0;//앵귤러 = 회전, +는 시계방향 -는 반시계방향이라 그냥 없앨려고 0값 줌.

        StartCoroutine(LevelUpRoutine());
    }
    IEnumerator LevelUpRoutine() //IEnumerator 열거형 인터페이스 (코루틴 쓸 때 사용)
    {
        yield return new WaitForSeconds(0.2f);

        anim.SetInteger("Level", level +1); // 공 생긴 것(합쳐진 것) 레벨 높히는 것 (애니메이터에서 몇레벨값이 나올지)
        // 위에 +1을 ++로 안하는 이유가 연산 걸리는 시간을 늦춰서 레벨이 올라가는 것을 너무 빠르게 하는 걸 막음.

        EffectPlay(); // 레벨업하면서 이펙트 효과 나오게
        manager.SfxPlay(GameManager.Sfx.LevelUp);//레벨업하면서 소리나게
        yield return new WaitForSeconds(0.3f);
        level++; //이건 레벨값을 올리는거 (오브젝트 레벨값)

        manager.maxLevel = Mathf.Max(level, manager.maxLevel); //인자 값중에 최대값을 반환하는 함수

        isMerge = false; //합체 종료
    }

    private void OnTriggerStay2D(Collider2D collision) {
        if(collision.tag == "Finish") { //라인의 태그를 피니시로 줌
            deadTime += Time.deltaTime; //시간 더해주는거 차이없게 델타로

            //2초가 지나면 빨갛게
            if(deadTime > 2){ //경고
                //빨갛게
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.1f); //color(red, greem, blue) -> rgb값
            }
            if(deadTime > 5){ //게임아웃
                //빨갛게
                manager.GameOver(); //매니저의 게임오버함수 불러옴
            }
        }
    }

    //합체할때 이펙트 나오게 함수 새로 만듦.
    void EffectPlay()
    {  // 파티클 위치와 크기를 보정해주는 함수 생성
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        effect.Play();
    }

    private void OnTriggerExit2D(Collider2D collision) { //경계면 붙어있다가 탈출시 정상화
        if(collision.tag == "Finish") {
            deadTime = 0;
            spriteRenderer.color = Color.white; //기본 색깔
        }
    }
}
