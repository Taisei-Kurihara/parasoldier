using TMPro;
using UnityEngine;

public class te : MonoBehaviour
{
    TextMeshProUGUI textMesh;
    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }
    private void Update()
    {
        textMesh.text = texttest.Instance.text;
    }
}
