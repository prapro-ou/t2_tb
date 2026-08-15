using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using PlayEveryWare.EpicOnlineServices;
public class ModuleColorOrchestrator : MonoBehaviour
{
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private SpriteRenderer _neon;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private List<Component> _colorComponents;

    public async UniTask Initialize()
    {
        ColorChange(_moduleDataOrchestrator.UserColors.Values.ToList()).Forget();
        _moduleDataOrchestrator.UserColors.TryGetValue(EOSManager.Instance.GetProductUserId(), out Color userColor);
        _text.color = userColor;
        if (_colorComponents != null && _colorComponents.Count > 0)
        {
            foreach (Component component in _colorComponents)
            {
                if (!component.TryDOColor(userColor, 1.0f, out var tweener))
                {
                    Debug.Log("ColorChange task was canceled.");
                    continue;
                }
            }
        }

        await UniTask.WaitForSeconds(1.0f);
    }

    private async UniTask ColorChange(List<Color> userColor)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        try
        {
            _neon.DOKill();
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_moduleDataOrchestrator.IsSuccess)
                {
                    _text.color = Color.white;
                    await _neon.DOColor(Color.white, 0.25f)
                               .ToUniTask(cancellationToken: cancellationToken);
                    break;
                }
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