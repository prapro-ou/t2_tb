using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
public class SceneBlockTransitionOrchestrator : MonoBehaviour
{
    [SerializeField] private Transform _maskTF;
    [SerializeField] private GameObject _blockObject;

    public async UniTask SceneIn()
    {
        _blockObject.SetActive(true);
        await _maskTF.DOScale(150, 1f);
    }

    public async UniTask SceneOut()
    {
        _blockObject.SetActive(true);
        await _maskTF.DOScale(0, 1f);
    }
}