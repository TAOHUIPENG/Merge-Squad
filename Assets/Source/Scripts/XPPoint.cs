
using System.Collections;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class XPPoint : MonoBehaviour
{
    [SerializeField] private float xp;
    [SerializeField] private float flyDuration = 0.6f;

    private bool _pickedUp;
    private Coroutine _flyCoroutine;

    public void Init(Vector3 originPoint, Vector3 playerPoint)
    {
        // Initial pop/bounce force
        Vector3 force = (Mathf.Sign(playerPoint.x - originPoint.x) == -1 ? Vector3.right : Vector3.left) + Vector3.up;
        GetComponent<Rigidbody>().AddForce(force * _gameData.pickUpFlyForce);

        // After the initial bounce delay, start flying to the EXP progress bar
        Invoke(nameof(StartFlyToBar), _gameData.timeBeforeXPActivate);
    }

    private void StartFlyToBar()
    {
        _flyCoroutine = StartCoroutine(FlyCoroutine());
    }

    private IEnumerator FlyCoroutine()
    {
        var bar = FindObjectOfType<PlayerTimeFinishBar>();
        if (bar == null) yield break;

        // Stop physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Cache canvas camera once — canvas render mode won't change at runtime
        var canvas = bar.GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera
            : null;

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            Camera cam = Camera.main;
            if (cam != null)
            {
                // Recalculate every frame — tracks the current fill-tip position of the bar
                Vector3 fillWorldPos = bar.GetFillEndWorldPosition();
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, fillWorldPos);
                float depth = cam.WorldToScreenPoint(transform.position).z;
                Vector3 targetWorld = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));

                // InQuad-like acceleration toward the bar
                float lerpSpeed = Mathf.Lerp(3f, 25f, t * t);
                transform.position = Vector3.Lerp(transform.position, targetWorld, Time.deltaTime * lerpSpeed);
            }

            // Scale down during the last 40% of the flight
            if (t > 0.6f)
            {
                float scaleT = (t - 0.6f) / 0.4f;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);
            }

            yield return null;
        }

        PickUpInternal();
    }

    public float PickUp()
    {
        if (_pickedUp) return 0f;
        _pickedUp = true;
        if (_flyCoroutine != null) StopCoroutine(_flyCoroutine);
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