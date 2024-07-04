using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseControlsObjectRotatio : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void OnMouseDrag()
    {
        float mouseX = Input.GetAxis("Mouse X") * 10.0f;
        this.transform.Rotate(new Vector3(0, -mouseX,0));
    }
    // void OnMouseDrag()
    // {
    //     float mouseX = Input.GetAxis("Mouse X");
    //     this.transform.Rotate(new Vector3(0, -mouseX,0));
    // }
}
