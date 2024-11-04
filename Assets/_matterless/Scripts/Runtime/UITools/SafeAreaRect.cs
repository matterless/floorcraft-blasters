using UnityEngine;
 
public class SafeAreaRect : MonoBehaviour
{
 
    private RectTransform _rectTransform;
 
    void Awake()
    {
        Refresh();
    }
 
    void Update()
    {
     
    }
    
    private void OnRectTransformDimensionsChange()
    {
        Refresh();
    }
    
    void Refresh()
    {
        if(_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        var anchorMin = Screen.safeArea.position;
        var anchorMax = Screen.safeArea.position + Screen.safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
 
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
 
}