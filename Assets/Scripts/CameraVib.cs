using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVib : MonoBehaviour
{
    public float strength = 0.1f;

    private bool isVibrating = false;
    private Coroutine vib;

    private Vector3 originalPosition;

    public void StartVibration()
    {
        if (!isVibrating)
        {
            vib = StartCoroutine(Vibrate());
        }
    }
    public void StopVibration()
    {
        if (vib!=null)
        {
            StopCoroutine(vib);

            transform.position = originalPosition;
            isVibrating = false;
        }
    }

    private IEnumerator Vibrate()
    {
        isVibrating = true;

        originalPosition = transform.position;
        float elapsedTime = 0f;

        while (true)
        {
            float x = originalPosition.x + Random.Range(-strength, strength);
            float y = originalPosition.y + Random.Range(-strength, strength);
            transform.position = new Vector3(x, y, originalPosition.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        
    }
}
