using UnityEngine;
using System.Collections;

public class RainEffect : MonoBehaviour
{
    [Header("Rain Settings")]
    public int lightRainRate = 500;
    public int heavyRainRate = 1500;
    public float rainAreaSize = 50f;
    public float rainHeight = 30f;
    public float dropSpeed = 50f;
    public float rainLifetime = 2f;

    [Header("Sky Settings")]
    public Color clearSky = new Color(0.8f, 0.9f, 1f);
    public Color lightSky = new Color(0.65f, 0.75f, 0.85f);
    public Color heavySky = new Color(0.1f, 0.1f, 0.15f);

    [Header("Weather Cycle Settings")]
    public float minWeatherDuration = 5f;
    public float maxWeatherDuration = 20f;

    private ParticleSystem rainParticleSystem;
    private ParticleSystem.EmissionModule emission;
    private int currentRate = 0;

    void Start()
    {
        GameObject rainGO = new GameObject("RainSystem");
        rainGO.transform.SetParent(transform);
        rainGO.transform.localPosition = new Vector3(0, rainHeight, 0);

        rainParticleSystem = rainGO.AddComponent<ParticleSystem>();
        var main = rainParticleSystem.main;
        emission = rainParticleSystem.emission;
        var shape = rainParticleSystem.shape;
        var velocityOverLifetime = rainParticleSystem.velocityOverLifetime;
        var noise = rainParticleSystem.noise;

        // Configure main settings
        main.startSpeed = 0;
        main.startLifetime = rainLifetime;
        main.startSize = 0.05f;
        main.startColor = Color.white;
        main.maxParticles = 3000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission
        emission.rateOverTime = 0;

        // Shape (box above player or scene)
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize, 1f, rainAreaSize);

        // Velocity: downward rain
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.y = -dropSpeed;

        // Noise adds jitter/randomness
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.1f;

        // Renderer config
        var renderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        renderer.material.color = new Color(1f, 1f, 1f, 0.5f);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5f;

        rainParticleSystem.Play();

        StartCoroutine(WeatherCycle());
    }

    void Update()
    {
        if (Camera.main != null)
        {
            float t = Mathf.InverseLerp(0f, heavyRainRate, currentRate);

            // Blend background based on rain intensity
            if (currentRate == 0)
                Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, clearSky, Time.deltaTime);
            else if (currentRate <= lightRainRate)
                Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, lightSky, Time.deltaTime);
            else
                Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, heavySky, Time.deltaTime);
        }
    }

    IEnumerator WeatherCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWeatherDuration, maxWeatherDuration));

            int choice = Random.Range(0, 3); // 0 = clear, 1 = light, 2 = heavy

            switch (choice)
            {
                case 0:
                    SetRainIntensity(0); // no rain
                    break;
                case 1:
                    SetRainIntensity(lightRainRate);
                    break;
                case 2:
                    SetRainIntensity(heavyRainRate);
                    break;
            }
        }
    }

    public void SetRainIntensity(int newRate)
    {
        currentRate = newRate;
        emission.rateOverTime = currentRate;
    }
}