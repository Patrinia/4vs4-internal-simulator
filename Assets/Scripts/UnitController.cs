using UnityEngine;

public class UnitController : MonoBehaviour // 게임 오브젝트에 붙어야 하므로 MonoBehaviour 상속
{
    [Header("Unit Data File")]
    public UnitData unitData; // 아까 만든 SO 데이터 파일을 연결할 빈칸입니다.

    [Header("Current Status (In Game)")]
    public int currentHp;     // 현재 체력 (게임 중 데미지를 입으면 깎임)
    public int currentTurnSpeed; // 이번 턴에 결정된 행동 속도

    void Start()
    {
        InitializeUnit();
    }

    // 유닛이 처음 맵에 생성될 때 스탯을 세팅하는 함수
    public void InitializeUnit()
    {
        // 1. SO 원본 데이터에서 최대 체력을 가져와 현재 체력에 꽉 채워줍니다.
        currentHp = unitData.maxHp;

        // 2. 스피드 계산 함수를 실행합니다.
        RollSpeed();

        Debug.Log($"{unitData.unitName} 생성! HP: {currentHp}, 턴 속도: {currentTurnSpeed}");
    }

    // 최소~최대 속도 사이에서 무작위로 이번 턴의 속도를 정하는 함수
    public void RollSpeed()
    {
        // Random.Range에 정수(int)를 넣을 때, 뒷 숫자(최대값)는 뽑기에서 제외됩니다.
        // 따라서 maxSpeed도 포함해서 뽑히게 하려면 뒤에 + 1을 해주어야 합니다.
        currentTurnSpeed = Random.Range(unitData.minSpeed, unitData.maxSpeed + 1);
    }
}