namespace OriginalNameSpace.Param
{

    // Param構造体一覧
    [System.Serializable]
    // スキルデータ
    public struct SkillBaseData : IParamPhantom
    {
        public float SkillStrength;
        public float CastTime;
        public float ReChargeTime;
        public int CastCount;
    }

    [System.Serializable]
    public struct BarrageData
    {
        public DamageData DamageData;
        public float BarrageSpeed;
    }

    // ダメージ
    [System.Serializable]
    public struct DamageData : IParamPhantom
    {
        public DamageTypeEnum DamageType; //ダメージタイプ
        public float DamageValue; //ダメージ量 
        public float CriticalHitRate; //クリティカルヒット率
        public float CriticalDamageMulti; //クリティカルダメージ倍率
    }

    // キャラクター
    [System.Serializable]
    public struct CharacterBaseData
    {
        public float MaxHP;
        public float Speed;
    }
}