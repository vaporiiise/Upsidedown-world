using UnityEngine;
using UnityEngine.InputSystem; 

public class GlobalFlipController : MonoBehaviour
{
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            GravityManager.OnGlobalFlip?.Invoke();
            
        }
    }
}