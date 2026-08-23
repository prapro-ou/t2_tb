using UnityEngine;

// ModuleToolsOrchestratorIndividual<T> を継承し、Tに構造体を渡します
public class WireToolsOrchestrator : ModuleToolsOrchestratorIndividual<WireModuleSettingData>
{
    // このクラス自体は継承だけでOKです！
    // 内部でEOS通信や初期化メソッド登録用の仕組みが自動で動きます。
}