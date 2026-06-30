using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AnimalImageTracker : MonoBehaviour
{
    [System.Serializable]
    public class AnimalMarker
    {
        public string markerName;
        public GameObject animalPrefab;
        public AudioClip animalSound;
    }

    public List<AnimalMarker> animals = new List<AnimalMarker>();

    private ARTrackedImageManager trackedImageManager;
    private GameObject currentAnimal;
    private AudioClip currentSound;
    private AudioSource audioSource;

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (ARTrackedImage image in args.added)
            ShowAnimalForMarker(image);

        foreach (ARTrackedImage image in args.updated)
        {
            if (image.trackingState == TrackingState.Tracking && currentAnimal != null)
            {
                currentAnimal.transform.position = image.transform.position;
                currentAnimal.transform.rotation = image.transform.rotation;
            }
        }

        foreach (ARTrackedImage image in args.removed)
            ClearAnimal();
    }

    private void ShowAnimalForMarker(ARTrackedImage image)
    {
        string markerName = image.referenceImage.name;

        foreach (AnimalMarker animal in animals)
        {
            if (animal.markerName == markerName)
            {
                ClearAnimal();

                currentAnimal = Instantiate(
                    animal.animalPrefab,
                    image.transform.position,
                    image.transform.rotation
                );

                currentSound = animal.animalSound;
                return;
            }
        }

        Debug.LogWarning("Nenhum animal configurado para o marker: " + markerName);
    }

    public void PlayCurrentAnimalSound()
    {
        if (currentSound != null)
            audioSource.PlayOneShot(currentSound);
    }

    public GameObject GetCurrentAnimal()
    {
        return currentAnimal;
    }

    public void ClearAnimal()
    {
        if (currentAnimal != null)
            Destroy(currentAnimal);

        currentAnimal = null;
        currentSound = null;
    }
}
