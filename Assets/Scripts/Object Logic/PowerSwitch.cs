using UnityEngine;

public class PowerSwitch : MonoBehaviour, IDamageable
{
    [SerializeField] private bool isActive = false;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color onColor = Color.green;
    [SerializeField] private Color offColor = Color.red;

    [SerializeField] private MonoBehaviour powerTarget; 

    private void Start()
    {
        UpdateVisuals();
    }

    public void Damage(float damage)
    {
        ToggleSwitch();
    }

    public void ToggleSwitch()
    {
        isActive = !isActive;
        UpdateVisuals();

        if (powerTarget is IPowerReceiver receiver)
        {
            receiver.OnPowerChanged(isActive);
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = isActive ? onColor : offColor;
    }

    public bool IsActive() => isActive;
}

public interface IPowerReceiver
{
    void OnPowerChanged(bool state);
}