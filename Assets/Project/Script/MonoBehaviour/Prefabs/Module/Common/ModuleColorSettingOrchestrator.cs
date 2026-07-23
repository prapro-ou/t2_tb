using System.Collections.Generic;
using UnityEngine;
using Epic.OnlineServices;
public class ModuleColorSettingOrchestrator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _light, _neon;
    public void Initialize(Dictionary<ProductUserId, Color> color)
    {

    }
}