using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class DialModuleRotationMediator : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler,
    IPointerEnterHandler,
    IPointerMoveHandler,
    IPointerExitHandler
{
    [SerializeField] private GameStatusActiveSOData _gameStatusActiveSOData; // ゲーム状態SO
    [SerializeField] private ModuleDataOrchestrator _moduleDataOrchestrator;
    [SerializeField] private Transform _dialTarget;    // 回転させたいダイヤルのTransform
    [SerializeField] private SpriteRenderer _dialRenderer;
    [Header("扇形判定の設定")]
    private float _fanAngle = 60f;   // 操作を受け付ける扇形の全開口角度（例: 90度なら上下に45度ずつ）
    private bool _isDragging;        // 扇形内でクリックされたかどうかの判定用フラグ
    private bool _isHoveredInFan;

    #region ========== IHandler ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHoverState(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateHoverState(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging) return;

        SetHoverState(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 1.マウス位置確認
        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData);
        if (!IsInFanAngle(mouseWorldPos))
        {
            _isDragging = false;
            return;
        }

        // 2.ドラッグ開始
        _isDragging = true;
        RotateToMouse(mouseWorldPos);

        SetHoverState(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 0.ドラッグスキップ
        if (!_isDragging) return; //範囲確認

        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData);
        RotateToMouse(mouseWorldPos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        SetHoverState(false);
    }

    #endregion ========== IHandler ==========

    #region ========== Public Function ==========

    /// <summary>
    /// マウス位置をもとに扇形ホバー状態を更新
    /// </summary>
    private void UpdateHoverState(PointerEventData eventData)
    {
        if (eventData == null) return;
        if (_isDragging)
        {
            if (!_isHoveredInFan)
            {
                SetHoverState(true);
            }
            return;
        }
        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData);
        bool isInside = IsInFanAngle(mouseWorldPos);
        // 状態が変化した時だけ演出メソッドを呼び出す
        if (isInside != _isHoveredInFan)
        {
            SetHoverState(isInside);
        }
    }

    /// <summary>
    /// ホバー状態の切り替えと演出実行
    /// </summary>
    private void SetHoverState(bool isHovered)
    {
        _isHoveredInFan = isHovered;

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

    /// <summary>
    /// マウス座標をワールド座標に変換
    /// </summary>
    private Vector3 GetMouseWorldPosition(PointerEventData eventData)
    {
        Vector3 screenPos = eventData.position;
        float distanceToCamera = Mathf.Abs(_gameStatusActiveSOData.Camera.transform.position.z - _dialTarget.position.z);
        screenPos.z = distanceToCamera;
        return _gameStatusActiveSOData.Camera.ScreenToWorldPoint(screenPos);
    }

    /// <summary>
    /// マウスとの角度計算
    /// </summary>
    private float CalculateAngleFromDialCenter(Vector3 targetWorldPos)
    {
        Vector2 dir = targetWorldPos - _dialTarget.position;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// マウスがある方向へダイヤルを直接向かせる
    /// </summary>
    private void RotateToMouse(Vector3 mouseWorldPos)
    {
        float mouseAngle = CalculateAngleFromDialCenter(mouseWorldPos);

        // CalculateAngleFromDialCenter は右方向(X軸正方向)を 0 度として返すため、
        // そのまま Z 軸の回転にセットすれば「ダイヤルの右側」がマウス方向を直接向きます
        _dialTarget.rotation = Quaternion.Euler(0f, 0f, mouseAngle);
    }

    /// <summary>
    /// マウス座標がダイヤル右側を基準とした扇形範囲内にあるか判定
    /// </summary>
    private bool IsInFanAngle(Vector3 mouseWorldPos)
    {
        Vector2 dirToMouse = (mouseWorldPos - _dialTarget.position).normalized;
        if (dirToMouse == Vector2.zero) return false;
        float angle = Vector2.Angle(_dialTarget.right, dirToMouse);
        return angle <= (_fanAngle / 2f);
    }

    // Sceneビューで右側の扇形範囲を確認するためのデバッグ表示
    private void OnDrawGizmosSelected()
    {
        if (_dialTarget == null) return;
        Gizmos.color = Color.cyan;
        Vector3 center = _dialTarget.position;
        Vector3 right = _dialTarget.right; // 右方向を基準に描画
        Vector3 topRayDir = Quaternion.Euler(0, 0, _fanAngle / 2f) * right;
        Vector3 bottomRayDir = Quaternion.Euler(0, 0, -_fanAngle / 2f) * right;
        Gizmos.DrawRay(center, topRayDir * 60f);
        Gizmos.DrawRay(center, bottomRayDir * 60f);
    }
    #endregion ========== Private Function ==========
}