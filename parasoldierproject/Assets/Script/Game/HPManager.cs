using UnityEngine;
using UnityEngine.UI;

public enum PType
{
    P1, // プレイヤー1
    P2  // プレイヤー2
}

public class HPManager : MonoBehaviour
{
    [SerializeField]
    Image P1Hp; // HP表示UI
    [SerializeField]
    Image P1gauge; // HP表示UI
    [SerializeField]
    Image P2Hp; // HP表示UI
    [SerializeField]
    Image P2gauge; // HP表示UI



}
