import os
import glob
import pandas as pd
from sklearn.preprocessing import StandardScaler

# ====================================================
# [DataLoader]
# 수많은 전투 로그(CSV)를 찾아내어 하나의 거대 행렬로 병합하고,
# 노이즈(짧은 전투)를 제거한 뒤, 머신러닝이 소화할 수 있도록 정규화하는 클래스입니다.
# ====================================================
class DataLoader:
    # 1. 초기화 메서드
    # [경로 수정 가이드] 
    # 현재 이 파일은 Python_Analytics 폴더 안에 있으므로, 
    # ../ 를 사용하여 상위 폴더로 한 칸 나간 뒤 SimulationLogs로 찾아갑니다.
    def __init__(self, folder_path="../SimulationLogs/MLCluster"):
        self.folder_path = folder_path
        self.scaler = StandardScaler() 
        self.raw_data = None
        
        # ====================================================
        # [피처 수정 가이드] 매우 중요!
        # 만약 유니티(C#)에서 새로운 데이터를 추가로 추출하게 되었다면,
        # (예: Ultimate_Count 라는 칼럼을 추가했다면)
        # 반드시 아래 리스트의 끝에 'Ultimate_Count'라고 이름을 똑같이 추가해 주셔야 합니다.
        # 반대로 C#에서 데이터를 뺐다면 여기서도 지워주시면 됩니다.
        # ====================================================
        self.feature_columns = [
            'Total_Rounds', 'Alive_Ratio', 'Remaining_HP_Ratio', 'YinYang_Deviation',
            'Skill_Aggressive', 'Skill_Heal', 'Skill_Utility', 'Skill_Defensive',
            'Turn_Skip_Ratio', 'Corrosion_Revert_Ratio'
        ]

    # 2. 파일 병합 (Data Ingestion)
    def load_and_merge_csvs(self):
        # 폴더 내 모든 .csv 파일을 검색합니다.
        search_pattern = os.path.join(self.folder_path, "**", "*.csv")
        csv_files = glob.glob(search_pattern, recursive=True)

        if not csv_files:
            raise FileNotFoundError(f"'{self.folder_path}' 폴더에서 CSV 파일을 찾을 수 없습니다. 경로를 확인하세요.")

        # 찾은 파일들을 세로로 길게 이어 붙입니다.
        df_list = [pd.read_csv(file) for file in csv_files]
        self.raw_data = pd.concat(df_list, ignore_index=True)
        
        print(f"[1/3] 파일 로드 완료: 총 {len(csv_files)}개의 파일을 병합했습니다. (총 전투 기록: {len(self.raw_data)}개)")
        return self.raw_data

    # 3. 노이즈 필터링 (Cleaning)
    # [수정 가이드] 기획적으로 버려야 할 기준 라운드가 3라운드에서 5라운드로 바뀌었다면, 
    # 사용할 때 filter_noise(min_rounds=5) 처럼 괄호 안의 숫자만 바꿔서 호출하면 됩니다.
    def filter_noise(self, min_rounds=3):
        if self.raw_data is None:
            raise ValueError("데이터가 없습니다. load_and_merge_csvs()를 먼저 실행하세요.")

        before_count = len(self.raw_data)
        
        # 기획 규칙: 총 라운드 수가 지정된 라운드(기본값 3) 이상인 데이터만 남깁니다.
        self.raw_data = self.raw_data[self.raw_data['Total_Rounds'] >= min_rounds]
        
        after_count = len(self.raw_data)
        print(f"[2/3] 필터링 완료: {before_count - after_count}개의 유효하지 않은 짧은 전투(노이즈)가 제거되었습니다. (남은 데이터: {after_count}개)")
        return self.raw_data

    # 4. 데이터 스케일링 및 분리 (Standardization)
    def get_scaled_features(self):
        # Sim_ID나 다른 문자가 학습에 들어가지 않도록, 우리가 지정한 피처(feature_columns)만 쏙 빼냅니다.
        features = self.raw_data[self.feature_columns]
        
        # 데이터를 평균 0, 표준편차 1로 압축 평탄화합니다.
        scaled_matrix = self.scaler.fit_transform(features)

        # 다시 보기 좋은 표(DataFrame) 형태로 변환합니다.
        scaled_df = pd.DataFrame(scaled_matrix, columns=self.feature_columns, index=self.raw_data.index)
        
        # 나중에 누구의 데이터인지 식별하기 위해 정답지(ID)는 따로 반환합니다.
        sim_ids = self.raw_data['Sim_ID']

        print("[3/3] 정규화 완료: 데이터 스케일링 성공. 머신러닝 학습 준비 완료.")
        return scaled_df, sim_ids