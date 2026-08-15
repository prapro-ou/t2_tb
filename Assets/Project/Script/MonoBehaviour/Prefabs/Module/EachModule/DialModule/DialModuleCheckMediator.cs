using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class DialModuleCheckMediator : MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData; // ゲーム状態SO
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private DialModuleLockMediator _dialModuleLockMediator;
    [SerializeField] private Transform _dialCore;
    [SerializeField] private SpriteRenderer _dialRenderer;

    public void Initialize()
    {
        _moduleDataOrchestrator.ThisModuleSettingData.ModuleGroup.TryGetValue(_moduleDataOrchestrator.ThisModuleSettingData.ModuleVersion, out var userId);
        _moduleDataOrchestrator.UserColors.TryGetValue(userId, out Color userColor);
        _dialRenderer.color = userColor;
    }

    #region ========== IHandler ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverState(true);
        _dialCore.DOLocalMoveY(5f, 0.1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverState(false);
        _dialCore.DOLocalMoveY(0f, 0.1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _dialModuleLockMediator.Click();
        ClickAnimation().Forget();
    }

    private async UniTask ClickAnimation()
    {
        await _dialCore.DOLocalMoveY(0f, 0.1f);
        await _dialCore.DOLocalMoveY(5f, 0.1f);
    }

    #endregion ========== IHandler ==========

    #region ========== Public Function ==========

    /// <summary>
    /// ホバー状態の切り替えと演出実行
    /// </summary>
    private void SetHoverState(bool isHovered)
    {
        if (isHovered)
        {
            _dialRenderer.DOColor(Color.white, 0.25f);
        }
        else
        {
            _moduleDataOrchestrator.ThisModuleSettingData.ModuleGroup.TryGetValue(_moduleDataOrchestrator.ThisModuleSettingData.ModuleVersion, out var userId);
            _moduleDataOrchestrator.UserColors.TryGetValue(userId, out Color userColor);
            _dialRenderer.DOColor(userColor, 0.25f);
        }
    }

    #endregion

    #region ========== Private Function ==========

    #endregion ========== Private Function ==========
}