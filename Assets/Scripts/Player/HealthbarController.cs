using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Height above head in world units

    [Header("Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private ParticleSystem healingParticles;

    [Header("Damage Feedback")]
    [SerializeField] private float uiShakeIntensity = 10f; // Higher values for Screen Space
    [SerializeField] private Color hurtColor = Color.red;

    [Header("Healing Feedback")]
    [SerializeField] private Color healColor = Color.cyan;
    [SerializeField] private float healPulseScale = 1.2f;
    [SerializeField] private float vfxDuration = 1.0f;

    private Color _originalColor;
    private Vector3 _originalScale;
    private Coroutine _activeRoutine;

    void Awake()
    {
        _originalColor = fillImage.color;
        _originalScale = barRoot.localScale;
        canvasGroup.alpha = 0;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Convert World Position to Screen Pixels
        Vector3 worldPos = playerTransform.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // If the player is behind the camera, hide the UI
        if (screenPos.z < 0)
        {
            canvasGroup.alpha = 0;
            return;
        }

        // Apply the position to the UI
        transform.position = screenPos;
    }

    public void OnHealthChanged(float currentHealth, float maxHealth, bool isHealing = false)
    {
        fillImage.fillAmount = currentHealth / maxHealth;
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(isHealing ? HealFeedback() : DamageFeedback());
    }

    private IEnumerator DamageFeedback()
    {
        canvasGroup.alpha = 1f;
        fillImage.color = hurtColor;

        Vector3 startPos = barRoot.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            barRoot.localPosition = startPos + (Vector3)Random.insideUnitCircle * uiShakeIntensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        barRoot.localPosition = startPos;
        fillImage.color = _originalColor;
        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator HealFeedback()
    {
        canvasGroup.alpha = 1f;
        fillImage.color = healColor;
        if (healingParticles != null) healingParticles.Play();

        barRoot.localScale = _originalScale * healPulseScale;
        yield return new WaitForSeconds(0.1f);
        barRoot.localScale = _originalScale;

        fillImage.color = _originalColor;
        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1.5f);
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
    }
}