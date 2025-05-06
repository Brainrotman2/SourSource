using System.Collections;
using UnityEngine;

public class water : MonoBehaviour
{
    public Material waterMaterial;        // Reference to the material used for the water
    public float fadeDuration = 20f;      // Duration of the fade (seconds)
    public float fadeIntervalMin = 60f;   // Minimum interval time between fades (seconds)
    public float fadeIntervalMax = 90f;   // Maximum interval time between fades (seconds)
    private bool isFading = false;        // To track if the fading is in progress
    private Color originalColor;          // To store the original color of the water material
    private Color fadedColor;             // To store the faded color of the water material

    void Start()
    {
        // Get the original color of the water material (assuming the alpha is set to 1 initially)
        originalColor = waterMaterial.color;
        fadedColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f); // Transparent color
        StartCoroutine(FadeWaterRandomly());
    }

    IEnumerator FadeWaterRandomly()
    {
        while (true)
        {
            // Wait for a random time before starting to fade
            float waitTime = Random.Range(fadeIntervalMin, fadeIntervalMax);
            yield return new WaitForSeconds(waitTime);

            // Start fading the water
            if (!isFading)
            {
                StartCoroutine(FadeWater());
            }
        }
	}

    IEnumerator FadeWater()
    {
        isFading = true;

        // Fade In: From transparent to full opacity
        float timeElapsed = 0f;
        while (timeElapsed < fadeDuration)
        {
            waterMaterial.color = Color.Lerp(fadedColor, originalColor, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        waterMaterial.color = originalColor; // Ensure it ends exactly at the original color

        // Wait for the fade duration to hold the full opacity
        yield return new WaitForSeconds(fadeDuration);

        // Fade Out: From full opacity to transparent
        timeElapsed = 0f;
        while (timeElapsed < fadeDuration)
        {
            waterMaterial.color = Color.Lerp(originalColor, fadedColor, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        waterMaterial.color = fadedColor; // Ensure it ends exactly at the faded color

        // Reset flag so it can fade again later
        isFading = false;
    }
}