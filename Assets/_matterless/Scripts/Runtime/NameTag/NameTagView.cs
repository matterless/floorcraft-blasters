using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NameTagView : MonoBehaviour
{
    public  uint entityId => m_EntityId;
    private uint m_EntityId;
    public string nameTag => m_NameText.text;
    [SerializeField] private TextMeshPro m_NameText;
    public void SetName(string name) => m_NameText.text = name;
    public void SetFontSize(float fontSize) => m_NameText.fontSize = fontSize;
    public void SetEntityId(uint id) => m_EntityId = id;

    private void Update()
    {
        transform.LookAt(Camera.main.transform.position);
        transform.Rotate(0, 180f, 0);
    }
}
