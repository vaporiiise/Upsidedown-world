using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeTrigger : MonoBehaviour
{
    // Reference the Impulse Source component
    private CinemachineImpulseSource _impulseSource;

    void Start()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void ShakeCamera()
    {
        // This triggers the shake based on the inspector settings
        _impulseSource.GenerateImpulse();
    }
    
    // Example: Shake with a specific velocity/strength
    public void ShakeCameraCustom(float force)
    {
        _impulseSource.GenerateImpulse(Vector3.one * force);
    }
}