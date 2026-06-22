using UnityEngine;

/// <summary>
/// Periodically spawns birds that fly across the arena as dynamic obstacles.
/// Place one instance in the scene — no prefab required, birds are built at runtime.
/// </summary>
public class BirdSpawner : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Minimum seconds between bird spawns")]
    public float minInterval = 5f;
    [Tooltip("Maximum seconds between bird spawns")]
    public float maxInterval = 12f;

    [Header("Flight Path")]
    [Tooltip("Vertical range for random spawn height")]
    public float minHeight = 1.5f;
    public float maxHeight = 4.5f;

    [Header("Speed")]
    public float minSpeed = 3f;
    public float maxSpeed = 6f;

    [Header("Flock")]
    [Tooltip("Occasionally spawn a small flock of 2-3 birds")]
    public bool enableFlocks = true;

    private float _nextSpawnTime;

    void Start()
    {
        ScheduleNext();
    }

    void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnBird();
            ScheduleNext();
        }
    }

    void ScheduleNext()
    {
        _nextSpawnTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    void SpawnBird()
    {
        int count = (enableFlocks && Random.value < 0.25f) ? Random.Range(2, 4) : 1;
        for (int i = 0; i < count; i++)
        {
            float offsetY = i * 0.55f;
            SpawnSingle(offsetY);
        }
    }

    void SpawnSingle(float yOffset)
    {
        // Alternate direction each spawn for variety
        int dir = Random.value > 0.5f ? 1 : -1;
        float spawnX = dir == 1 ? -12f : 12f;
        float spawnY = Random.Range(minHeight, maxHeight) + yOffset;

        var go = new GameObject("Bird");
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        var bird        = go.AddComponent<BirdController>();
        bird.direction  = dir;
        bird.speed      = Random.Range(minSpeed, maxSpeed);
        bird.birdColor  = RandomBirdColor();
    }

    Color RandomBirdColor()
    {
        Color[] palette =
        {
            new Color(0.20f, 0.65f, 0.25f), // green
            new Color(0.85f, 0.45f, 0.10f), // orange
            new Color(0.25f, 0.50f, 0.85f), // blue
            new Color(0.80f, 0.20f, 0.20f), // red
            new Color(0.60f, 0.20f, 0.75f), // purple
        };
        return palette[Random.Range(0, palette.Length)];
    }
}
