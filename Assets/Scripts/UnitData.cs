using UnityEngine;

// 유니티 상단 메뉴 우클릭을 통해 이 데이터 파일을 쉽게 생성할 수 있게 해주는 속성입니다.
[CreateAssetMenu(fileName = "NewUnitData", menuName = "Make Units/Unit Data")]
public class UnitData : ScriptableObject // MonoBehaviour가 아닌 ScriptableObject를 상속받습니다.
{
    [Header("Basic Info")]
    public string unitName; // 유닛의 이름 (예: "용감한 전사", "독 거미")

    [Header("Stats")]
    public int maxHp;       // 최대 체력
    public int minSpeed;    // 속도 최소치
    public int maxSpeed;    // 속도 최대치
}