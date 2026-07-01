using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.VisualScripting;

[RequireComponent(typeof(ARTrackedImageManager))]
public class AnimalTrackingController : MonoBehaviour
{
    [Serializable]
    public class AnimalMarkerConfig
    {
        [Tooltip("Nome exato definido na Reference Image Library.")]
        public string markerName;

        [Tooltip("Prefab que será exibido quando este marker for reconhecido.")]
        public GameObject animalPrefab;

        [Header("Transformação")]
        [Tooltip("Escala usada para este modelo.")]
        public float scale = 0.001f;

        [Tooltip("Distância à frente da câmera, em metros.")]
        public float distanceFromCamera = 1.2f;

        [Tooltip("Deslocamento vertical relativo à câmera.")]
        public float verticalOffset = -0.15f;

        [Tooltip("Correção de rotação específica deste modelo.")]
        public Vector3 rotationOffset = new Vector3(0f, 180f, 0f);
    }

    [Header("Configuração dos animais")]
    [SerializeField]
    private List<AnimalMarkerConfig> animals =
        new List<AnimalMarkerConfig>();

    [Header("Câmera")]
    [SerializeField]
    private Camera arCamera;

    private ARTrackedImageManager trackedImageManager;

    private readonly Dictionary<TrackableId, GameObject> spawnedAnimals =
        new Dictionary<TrackableId, GameObject>();

    public GameObject CurrentAnimal { get; private set; }

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            Debug.LogError(
                "AnimalTrackingController: nenhuma câmera AR foi encontrada."
            );
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged +=
            OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -=
            OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(
        ARTrackedImagesChangedEventArgs args)
    {
        foreach (ARTrackedImage trackedImage in args.added)
        {
            CreateAnimal(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in args.updated)
        {
            UpdateAnimal(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in args.removed)
        {
            RemoveAnimal(trackedImage);
        }
    }

    private void CreateAnimal(ARTrackedImage trackedImage)
    {
        if (spawnedAnimals.ContainsKey(trackedImage.trackableId))
        {
            return;
        }

        string detectedMarkerName =
            trackedImage.referenceImage.name;

        Debug.Log("Marker detectado: " + detectedMarkerName);

        AnimalMarkerConfig config =
            FindAnimalConfig(detectedMarkerName);

        if (config == null)
        {
            Debug.LogWarning(
                "Nenhum animal configurado para o marker: "
                + detectedMarkerName
            );
            return;
        }

        if (config.animalPrefab == null)
        {
            Debug.LogError(
                "O prefab do marker "
                + detectedMarkerName
                + " não foi configurado."
            );
            return;
        }

        if (arCamera == null)
        {
            Debug.LogError(
                "AnimalTrackingController: a câmera AR não está disponível."
            );
            return;
        }

        GameObject animal = Instantiate(config.animalPrefab);

        animal.name = "CurrentAnimal_" + detectedMarkerName;

        PositionAnimalInFrontOfCamera(animal, config);

        spawnedAnimals.Add(
            trackedImage.trackableId,
            animal
        );

        SetCurrentAnimal(animal);

        UpdateAnimal(trackedImage);
    }

    private AnimalMarkerConfig FindAnimalConfig(
        string detectedMarkerName)
    {
        foreach (AnimalMarkerConfig config in animals)
        {
            if (string.Equals(
                    config.markerName,
                    detectedMarkerName,
                    StringComparison.Ordinal))
            {
                return config;
            }
        }

        return null;
    }

    private void PositionAnimalInFrontOfCamera(
        GameObject animal,
        AnimalMarkerConfig config)
    {
        Vector3 spawnPosition =
            arCamera.transform.position
            + arCamera.transform.forward
                * config.distanceFromCamera
            + arCamera.transform.up
                * config.verticalOffset;

        animal.transform.position = spawnPosition;

        Quaternion cameraFacingRotation =
            Quaternion.Euler(
                0f,
                arCamera.transform.eulerAngles.y,
                0f
            );

        animal.transform.rotation =
            cameraFacingRotation
            * Quaternion.Euler(config.rotationOffset);

        animal.transform.localScale =
            Vector3.one * config.scale;
    }

    private void UpdateAnimal(ARTrackedImage trackedImage)
    {
        if (!spawnedAnimals.TryGetValue(
                trackedImage.trackableId,
                out GameObject animal))
        {
            return;
        }

        bool shouldBeVisible =
            trackedImage.trackingState
            == TrackingState.Tracking;

        animal.SetActive(shouldBeVisible);

        if (shouldBeVisible)
        {
            SetCurrentAnimal(animal);
        }
    }

    private void RemoveAnimal(ARTrackedImage trackedImage)
    {
        if (!spawnedAnimals.TryGetValue(
                trackedImage.trackableId,
                out GameObject animal))
        {
            return;
        }

        spawnedAnimals.Remove(trackedImage.trackableId);

        if (CurrentAnimal == animal)
        {
            ClearCurrentAnimal();
        }

        Destroy(animal);
    }

    private void SetCurrentAnimal(GameObject animal)
    {
        CurrentAnimal = animal;

        Variables.Scene(gameObject.scene).Set(
            "modelObject",
            animal
        );
    }

    private void ClearCurrentAnimal()
    {
        CurrentAnimal = null;

        Variables.Scene(gameObject.scene).Set(
            "modelObject",
            (GameObject)null
        );
    }
}
