
using DG.Tweening;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class XPPoint : MonoBehaviour
{
    [SerializeField] private float xp;
    [SerializeField] private float flyDuration = 0.6f;

    private bool _pickedUp = false;

    public void Init(Vector3 originPoint, Vector3 playerPoint)
    {
        // Initial pop/bounce force
        Vector3 force = (Mathf.Sign(playerPoint.x - originPoint.x) == -1 ? Vector3.right : Vector3.left) + Vector3.up;
        GetComponent<Rigidbody>().AddForce(force * _gameData.pickUpFlyForce);

        // After the initial bounce delay, fly to the EXP progress bar
        Invoke(nameof(StartFlyToBar), _gameData.timeBeforeXPActivate);
    }

    private void StartFlyToBar()
    {
        var bar = FindObjectOfType<PlayerTimeFinishBar>();
        if (bar == null) return;

        // Stop physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Convert UI bar position to world space
        Camera cam = Camera.main;
        if (cam == null) return;
        var canvas = bar.GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, bar.transform.position);
        float depth = cam.WorldToScreenPoint(transform.position).z;
        Vector3 targetWorld = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));

        // Fly to bar with easing, scale down on approach
        transform.DOMove(targetWorld, flyDuration).SetEase(Ease.InQuad);
        transform.DOScale(Vector3.zero, flyDuration * 0.4f)
            .SetDelay(flyDuration * 0.6f)
            .SetEase(Ease.InBack)
            .OnComplete(PickUpInternal);
    }

    public float PickUp()
    {
        if (_pickedUp) return 0f;
        _pickedUp = true;
        DOTween.Kill(transform);
        Destroy(gameObject);
        return xp;
    }

    private void PickUpInternal()
    {
        if (_pickedUp) return;
        _pickedUp = true;

        _audioManager?.PlayOneShot(_gameData.pickUpClip, 0.2f);
        _xpPicker?.OnPickUp?.Invoke(xp);

        Destroy(gameObject);
    }
}