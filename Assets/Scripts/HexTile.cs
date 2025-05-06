// Script for individual hex tiles with bobbing animation
using UnityEngine;

public class HexTile : MonoBehaviour
{
    public float bobAmplitude = 0.2f;
    public float bobFrequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * bobFrequency + transform.position.x + transform.position.z) * bobAmplitude;
        transform.position = new Vector3(startPos.x, startPos.y + offset, startPos.z);
    }
}