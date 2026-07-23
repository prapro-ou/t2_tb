using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
public abstract class ModuleToolsOrchestratorBase : MonoBehaviour
{
    public virtual async UniTask Initialize(ProductUserId productUserId, ModuleSettingData moduleSettingData)
    {

    }
}