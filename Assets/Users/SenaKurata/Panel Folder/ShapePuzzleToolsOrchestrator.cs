using UnityEngine;

// 3. ModuleToolsOrchestratorIndividual<T> を継承したコンポーネント
public class ShapePuzzleToolsOrchestrator : ModuleToolsOrchestratorIndividual<ShapePuzzleModuleSettingData>
{
    // コンポーネントとして Operator/ToolsOrchestrator にアタッチして使用します。
    // このスクリプト自体には追加の処理を書かなくても、継承元によってInspectorから
    // ShapePuzzleModuleSettingData を受け取るメソッドを紐付けられるようになります。
}