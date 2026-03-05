using UnityEngine;
using UnityEngine.Android;

public class RequestScenePermissionOnStart : MonoBehaviour
{
    private const string USESCENE = "com.oculus.permission.USE_SCENE";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(USESCENE))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => Debug.Log("granted");
            callbacks.PermissionDenied += _ => Debug.Log("denied");
            
            Permission.RequestUserPermission(USESCENE, callbacks);

        }
    }

    
}
