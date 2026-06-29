using UnityEngine;

public class Timer : MonoBehaviour
{
    public static Timer Instance { get; private set; }

    private float elapsedTime = 0f;
    private bool counterRunning = false;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        if (counterRunning)
            elapsedTime += Time.deltaTime;
    }

    public void StartCounter() {
        elapsedTime = 0f;
        counterRunning = true;
    }

    public void StopCounter() {
        counterRunning = false;
    }

    public float GetCounterValue() {
        return elapsedTime;
    }
}
