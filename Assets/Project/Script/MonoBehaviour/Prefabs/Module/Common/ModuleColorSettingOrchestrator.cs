using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PlayEveryWare.EpicOnlineServices;
public class ModuleColorSettingOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private SpriteRenderer _light, _neon;

    public void Start()
    {
        ColorChange(new List<Color>
        {
            Color.red,
            Color.green,
            Color.blue
        }).Forget();
    }

    public void Initialize()
    {
        ColorChange(_moduleDataOrchestrator.UserColors.Values.ToList()).Forget();
        _light.color = _moduleDataOrchestrator.UserColors[EOSManager.Instance.GetProductUserId()];
    }

    private async UniTask ColorChange(List<Color> userColor)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        try
        {
            _neon.DOKill();
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (Color color in userColor)
                {
                    await _neon.DOColor(Color.white, 0.25f)
                               .ToUniTask(cancellationToken: cancellationToken);
                    await _neon.DOColor(color, 0.25f)
                               .ToUniTask(cancellationToken: cancellationToken);
                    await UniTask.Delay(1000, cancellationToken: cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("ColorChange task was canceled.");
        }
    }

}