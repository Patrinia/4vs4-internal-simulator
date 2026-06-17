using UnityEngine;

// ====================================================
// [BrainFactory.cs]
// 기획자가 지정한 AIBrainType을 분석하여, 해당 지능에 걸맞은
// 최적의 AI 뇌(IUnitBrain) 객체를 생성하여 반환하는 전담 공장(Factory) 클래스입니다.
// ====================================================
public static class BrainFactory
{
    /// <summary>
    /// 엑셀(UnitData)에 정의된 AIBrainType에 따라 알맞은 IUnitBrain 객체를 동적으로 생성합니다.
    /// </summary>
    /// <param name="brainType">UnitData에 지정된 기본 AI 뇌 타입</param>
    /// <returns>타입에 맞는 IUnitBrain 인터페이스 구현체</returns>
    public static IUnitBrain CreateBrain(AIBrainType brainType)
    {
        switch (brainType)
        {
            case AIBrainType.Random:
                return new RandomActionBrain();

            case AIBrainType.Sequence:
                return new SequenceActionBrain();

            case AIBrainType.Strategic:
                return new StrategicActionBrain();

            case AIBrainType.Player:
                // [업데이트] 팩토리가 현재 환경을 스스로 인지합니다.
                // SimulationManager가 존재한다면 시뮬레이션 환경이므로 가상 데이터를 위해 임시 뇌를 장착합니다.
                if (SimulationManager.Instance != null)
                {
                    Debug.Log("<color=cyan>[BrainFactory] 시뮬레이션 환경 감지. Player 유닛에게 임시로 Strategic 뇌를 장착합니다.</color>");
                    return new StrategicActionBrain();
                }
                else
                {
                    // 실제 본 게임(A-M) 환경
                    // 추후 UI 입력을 기다리는 HumanBrain을 리턴할 수 있습니다.
                    return null;
                }

            case AIBrainType.MLAgent:
                // [E단계] 파이썬 연동 MLAgentBrain 스크립트 작성 시 이곳에서 리턴합니다.
                Debug.Log("<color=magenta>[BrainFactory] 머신러닝 뇌(MLAgent)가 선택되었으나 아직 구현되지 않았습니다. 임시로 Strategic 뇌를 장착합니다.</color>");
                return new StrategicActionBrain(); // 개발 중단 방지용 임시 뇌 장착

            default:
                // 기본 예외 처리는 무작위 행동으로 안전하게 떨어지게 합니다.
                Debug.Log($"<color=cyan>=== 브레인 타입값이 선언된 case문의 범위안에 없어서 랜덤 로직 적용되는 중, 현재 : {brainType}===</color>");
                return new RandomActionBrain();
        }
    }
}