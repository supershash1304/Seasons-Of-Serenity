using UnityEngine;

public class WeatherController : MonoBehaviour
{
    public enum WeatherType { Sun, Rain, Lava, Snow }
    public WeatherType currentWeather = WeatherType.Sun;

    public GameObject rainPrefab;
    public GameObject snowPrefab;
    public GameObject lavaPrefab;

    public float weatherRadius = 500f;

    private GameObject activeRain;
    private GameObject activeSnow;
    private GameObject activeLava;

    public void CycleWeather(Vector3 playerPosition)
    {
        // Cycle through weather types
        currentWeather = (WeatherType)(((int)currentWeather + 1) % System.Enum.GetValues(typeof(WeatherType)).Length);
        ApplyWeather(playerPosition);
    }

    public void ApplyWeather(Vector3 playerPosition)
    {
        // Clear previous weather
        if (activeRain != null) Destroy(activeRain);
        if (activeSnow != null) Destroy(activeSnow);
        if (activeLava != null) Destroy(activeLava);

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
                    activeLava.transform.localScale = new Vector3(weatherRadius * 2, 1f, weatherRadius * 2);
                }
                Debug.Log("Weather: Lava");
                break;
        }
    }
}
