using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class CombatCinematics : MonoBehaviour
{
    public static CombatCinematics Instance;

    [Header("Volume Reference")]
    public Volume targetVolume; // Drag your existing Volume GameObject here

    [Header("Timing Settings")]
    public float slowTimeScale = 0.1f;
    public float punchDuration = 0.1f;
    public float holdDuration = 0.2f;
    public float returnDuration = 0.4f;

    private Coroutine _activeRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Ensure the volume starts invisible
        if (targetVolume != null) targetVolume.weight = 0f;
    }

    public void TriggerKillEffect()
    {
        if (targetVolume == null) return;
        
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(KillSequence());
    }

    private IEnumerator KillSequence()
    {
        float elapsed = 0f;

        // 1. RAMP UP
        while (elapsed < punchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / punchDuration;

            Time.timeScale = Mathf.Lerp(1f, slowTimeScale, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            targetVolume.weight = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        Time.timeScale = slowTimeScale;
        targetVolume.weight = 1f;

        // 2. HOLD
        yield return new WaitForSecondsRealtime(holdDuration);

        // 3. RAMP DOWN
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / returnDuration;

            Time.timeScale = Mathf.Lerp(slowTimeScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            targetVolume.weight = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        // Final Reset
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        targetVolume.weight = 0f;
        _activeRoutine = null;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (targetVolume != null) targetVolume.weight = 0f;
    }
}