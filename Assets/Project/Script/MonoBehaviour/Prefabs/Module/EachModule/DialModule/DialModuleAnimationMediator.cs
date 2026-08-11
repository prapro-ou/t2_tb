using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
public class DialModuleAnimationMediator : MonoBehaviour
{
    [SerializeField] Transform _dialTransform;
    public async UniTask InitializeAnimation()
    {
        _dialTransform.rotation = Quaternion.Euler(0, 0, 15f);
        await _dialTransform.DORotate(new Vector3(0, 0, -3600), 5f, RotateMode.FastBeyond360).SetRelative().SetEase(Ease.OutQuad).ToUniTask();
    }
}