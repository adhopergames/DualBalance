using UnityEngine;

[RequireComponent(typeof(FallingObject))]
public class Orb : MonoBehaviour
{
    public enum OrbType { Light, Dark, Dual }
    public OrbType orbType;
}
