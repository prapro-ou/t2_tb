using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
public abstract class ModuleToolsOrchestratorBase : MonoBehaviour
{
    [SerializeField] private bool _isInitialize = true;
    public bool IsInitialize => _isInitialize;
    public virtual void Initialize(ProductUserId productUserId, List<ProductUserId> players, ModuleSettingData moduleSettingData)
    {

    }
    public void SetInitialize() => _isInitialize = true;
}