using UnityEngine;

public class BackgroundFlipManager : MonoBehaviour
{
    [Header("Colors")]
    public Color normalColor = new Color(0.1f, 0.1f, 0.1f);
    public Color flippedColor = new Color(0.2f, 0.05f, 0.2f);
    
    [Header("Settings")]
    public float transitionSpeed = 8f;
    
    private Camera _cam;
    private Color _targetColor;
    private bool _isFlipped = false;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _targetColor = normalColor;
        _cam.backgroundColor = normalColor;
    }

    // Subscribe to the global flip event
    private void OnEnable() => GravityManager.OnGlobalFlip += HandleColorFlip;
    private void OnDisable() => GravityManager.OnGlobalFlip -= HandleColorFlip;

    private void HandleColorFlip()
    {
        _isFlipped = !_isFlipped;
        _targetColor = _isFlipped ? flippedColor : normalColor;
    }

    void Update()
    {
        _cam.backgroundColor = Color.Lerp(_cam.backgroundColor, _targetColor, Time.deltaTime * transitionSpeed);
    }
}