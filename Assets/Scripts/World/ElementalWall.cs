using UnityEngine;

[RequireComponent(typeof(FallingObject))]
public class ElementalWall : MonoBehaviour
{
    public ElementType elementType;

    private void OnEnable()
    {
        ElementalAttackSystem.Instance?.RegisterWall(this);
    }

    private void OnDisable()
    {
        ElementalAttackSystem.Instance?.UnregisterWall(this);
    }
}
