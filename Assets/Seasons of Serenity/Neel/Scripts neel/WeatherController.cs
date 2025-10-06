using UnityEngine;

public class WeatherController : MonoBehaviour
{
    public enum WeatherType { Sun, Rain, Lava, Snow }
    public WeatherType currentWeather = WeatherType.Sun;

    public GameObject rainPrefab;
    public GameObject snowPrefab;
    public GameObject lavaPrefab;

    public float weatherRadius = 500f; // optional
    public float lavaScaleMultiplier = 2f; // optional scale control for lava

    private GameObject activeRain;
    private GameObject activeSnow;
    private GameObject activeLava;

    public void CycleWeather(Vector3 playerPosition)
    {
        currentWeather = (WeatherType)(((int)currentWeather + 1) % System.Enum.GetValues(typeof(WeatherType)).Length);
        ApplyWeather(playerPosition);
    }

    public void ApplyWeather(Vector3 playerPosition)
    {
        // --- Destroy previous weather with 2s delay ---
        if (activeRain != null) Destroy(activeRain, 2f);
        if (activeSnow != null) Destroy(activeSnow, 2f);
        if (activeLava != null) Destroy(activeLava, 2f);

        // --- Spawn new weather ---
        switch (currentWeather)
        {
            case WeatherType.Sun:
                Debug.Log("Weather: Sun");
                break;

            case WeatherType.Rain:
                if (rainPrefab != null)
                {
                    activeRain = Instantiate(rainPrefab, playerPosition, Quaternion.identity);
                }
                Debug.Log("Weather: Rain");
                break;

            case WeatherType.Snow:
                if (snowPrefab != null)
                {
                    activeSnow = Instantiate(snowPrefab, playerPosition, Quaternion.identity);
                }
                Debug.Log("Weather: Snow");
                break;

            case WeatherType.Lava:
                if (lavaPrefab != null)
                {
                    activeLava = Instantiate(lavaPrefab, playerPosition, Quaternion.identity);

                    // Keep prefab’s original scale, but apply multiplier
                    Vector3 originalScale = lavaPrefab.transform.localScale;
                    activeLava.transform.localScale = new Vector3(
                        originalScale.x * lavaScaleMultiplier,
                        originalScale.y,
                        originalScale.z * lavaScaleMultiplier
                    );
                }
                Debug.Log("Weather: Lava");
                break;
        }
    }
}
