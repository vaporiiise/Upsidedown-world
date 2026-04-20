using UnityEngine;

public class PowerDoor : MonoBehaviour, IPowerReceiver
{
    [Header("Settings")]
    [SerializeField] private int requiredPower = 1; 
    private int _currentPower = 0;

    [Header("Visuals")]
    [SerializeField] private GameObject doorGraphic;
    [SerializeField] private SpriteRenderer[] indicatorLights; 
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color inactiveColor = Color.gray;

    public void OnPowerChanged(bool state)
    {
        _currentPower += state ? 1 : -1;
        _currentPower = Mathf.Clamp(_currentPower, 0, requiredPower);

        UpdateIndicators();

        if (_currentPower >= requiredPower)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void UpdateIndicators()
    {
        for (int i = 0; i < indicatorLights.Length; i++)
        {
            indicatorLights[i].color = (i < _currentPower) ? activeColor : inactiveColor;
        }
    }

    private void OpenDoor()
    {
        doorGraphic.SetActive(false); 
        Debug.Log("Door Opened!");
    }

    private void CloseDoor()
    {
        doorGraphic.SetActive(true);
    }
}