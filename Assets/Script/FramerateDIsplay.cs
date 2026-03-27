using System;
using UnityEngine;
using TMPro;


public class FramerateDIsplay : MonoBehaviour
{
    public TextMeshProUGUI textMeshProUGUI;
    
    

    private void Update()
    {
        textMeshProUGUI.text = "Framerate: " + (1f / Time.deltaTime).ToString("00.00") + " frames";
    }
}
