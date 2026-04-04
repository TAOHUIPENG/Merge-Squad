using D2D;
using DG.Tweening;
using System;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;
public class XPPicker : MonoBehaviour
{
    [SerializeField] private float getDistance = 1f;
    [SerializeField] private float pickUpForce = 5f;
    [SerializeField] private GameObject pickUpVFX;

    [Header("经验条特效飞行目标")]
    [SerializeField] private float _xpFlyDuration = 0.35f;

    private PlayerTimeFinishBar _playerTimeFinishBar;

    private SexyOverlap overlap;

    public Action<float> OnPickUp;

    private float currentXPModifier = 1;

    private void Awake()
    {
        _xpPicker = this;
        overlap = GetComponent<SexyOverlap>();
    }

    private void Start()
    {
        var go = GameObject.Find("EXPProgress Bar");
        if (go != null)
            _playerTimeFinishBar = go.GetComponent<PlayerTimeFinishBar>();
        else
            Debug.LogWarning("[XPPicker] 未找到场景中的 'EXPProgress Bar' 物体，经验收集特效将在原地销毁");
    }
    private void FixedUpdate()
    {
        if (overlap.HasTouch)
        {
            foreach (var item in overlap.AllTouched)
            {
                if (item == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, item.transform.position);

                if (distance <= getDistance)
                {
                    var xp = item.GetComponent<XPPoint>().PickUp() * currentXPModifier;

                    _audioManager.PlayOneShot(_gameData.pickUpClip, .2f);
                    OnPickUp?.Invoke(xp);
                    PickVFX(item.transform.position);

                    return;
                }

                if (item.attachedRigidbody == null)
                {
                    return;
                }

                item.attachedRigidbody.isKinematic = true;

                float speed = pickUpForce - distance;
                speed = speed * Time.fixedDeltaTime;

                item.transform.position = Vector3.MoveTowards(item.transform.position, transform.position, speed);
            }
        }
    }
    private void PickVFX(Vector3 place)
    {
        var vfx = Instantiate(pickUpVFX, place, Quaternion.identity);

        // 禁用物理 & XPPoint 逻辑，避免干扰飞行动画
        var rb = vfx.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var xpScript = vfx.GetComponent<XPPoint>();
        if (xpScript != null) xpScript.enabled = false;

        var col = vfx.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_playerTimeFinishBar != null && Camera.main != null)
        {
            Vector3 targetWorld = _playerTimeFinishBar.GetFillEndWorldPosition();
            vfx.transform.DOMove(targetWorld, _xpFlyDuration)
                          .SetEase(Ease.InQuad)
                          .OnComplete(() => { if (vfx != null) Destroy(vfx); });
        }
        else
        {
            Destroy(vfx, 2f);
        }
    }

    public void IncreaseXPModifier()
    {
        currentXPModifier *= _gameData.xpModifierIncrease;
    }
}