using UnityEngine;

public class Paralax : MonoBehaviour
{
    public float parallaxMultiplier = 0.1f;

    private Material paralaxMaterial;
    private Transform player;
    private float lastplayerx;
    void Start()
    {
        paralaxMaterial = GetComponent<Renderer>().material;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastplayerx = player.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaY = player.position.y - lastplayerx;
        paralaxMaterial.mainTextureOffset += new Vector2(0, deltaY * parallaxMultiplier);
    }
}
