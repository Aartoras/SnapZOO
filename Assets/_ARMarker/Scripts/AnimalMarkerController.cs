using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AnimalMarkerController : MonoBehaviour
{
    public GameObject anteaterPrefab;
    public AudioClip anteaterSound;

    private GameObject currentAnimal;
    private AudioClip currentSound;
    private AudioSource audioSource;
    private ARTrackedImage trackedImage;

    private void Awake()
    {
        trackedImage = GetComponent<ARTrackedImage>();
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        string markerName = trackedImage.referenceImage.name;

        if (markerName == "anteater")
        {
            SpawnAnimal(anteaterPrefab, anteaterSound);
        }
        else
        {
            Debug.LogWarning("Marker sem animal configurado: " + markerName);
        }
    }

    private void SpawnAnimal(GameObject prefab, AudioClip sound)
    {
        currentAnimal = Instantiate(prefab, transform);
        currentAnimal.transform.localPosition = Vector3.zero;
        currentAnimal.transform.localRotation = Quaternion.identity;
        currentAnimal.transform.localScale = Vector3.one;

        currentSound = sound;
    }

    public void PlayAnimalSound()
    {
        if (currentSound != null)
            audioSource.PlayOneShot(currentSound);
    }

    public GameObject GetCurrentAnimal()
    {
        return currentAnimal;
    }
}
