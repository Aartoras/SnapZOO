using System;
using System.Collections.Generic;
using TMPro;
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
        [Header("Identificação do marker")]

        [Tooltip("Nome exato definido na Reference Image Library.")]
        public string markerName;

        [Header("Modelo e som")]

        [Tooltip("Prefab exibido quando este marker for reconhecido.")]
        public GameObject animalPrefab;

        [Tooltip("Som reproduzido pelo botão para este animal.")]
        public AudioClip animalSound;

        [Header("Informações do animal")]

        [Tooltip("Nome comum exibido no painel.")]
        public string displayName;

        [Tooltip("Nome científico exibido no painel.")]
        public string scientificName;

        [Tooltip("Bioma ou habitat principal.")]
        public string biome;

        [Tooltip("Tipo de alimentação.")]
        public string diet;

        [Tooltip("Nível de risco ou estado de conservação.")]
        public string risk;

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

    [Header("Câmera AR")]
    [SerializeField]
    private Camera arCamera;

    [Header("Painel de informações")]
    [SerializeField]
    private GameObject animalInfoPanel;

    [SerializeField]
    private TMP_Text animalNameText;

    [SerializeField]
    private TMP_Text scientificNameText;

    [SerializeField]
    private TMP_Text biomeText;

    [SerializeField]
    private TMP_Text dietText;

    [SerializeField]
    private TMP_Text riskText;

    private ARTrackedImageManager trackedImageManager;
    private AudioSource audioSource;

    private readonly Dictionary<TrackableId, GameObject> spawnedAnimals =
        new Dictionary<TrackableId, GameObject>();

    private readonly Dictionary<TrackableId, AnimalMarkerConfig> spawnedConfigs =
        new Dictionary<TrackableId, AnimalMarkerConfig>();

    public GameObject CurrentAnimal { get; private set; }

    private AudioClip currentSound;
    private AnimalMarkerConfig currentAnimalConfig;

    private void Awake()
    {
        trackedImageManager =
            GetComponent<ARTrackedImageManager>();

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

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Garante que o painel comece fechado.
        if (animalInfoPanel != null)
        {
            animalInfoPanel.SetActive(false);
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

        GameObject animal =
            Instantiate(config.animalPrefab);

        animal.name =
            "CurrentAnimal_" + detectedMarkerName;

        PositionAnimalInFrontOfCamera(
            animal,
            config
        );

        spawnedAnimals.Add(
            trackedImage.trackableId,
            animal
        );

        spawnedConfigs.Add(
            trackedImage.trackableId,
            config
        );

        SetCurrentAnimal(
            animal,
            config
        );

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

    private void UpdateAnimal(
        ARTrackedImage trackedImage)
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
            spawnedConfigs.TryGetValue(
                trackedImage.trackableId,
                out AnimalMarkerConfig config
            );

            SetCurrentAnimal(
                animal,
                config
            );
        }
        else if (CurrentAnimal == animal)
        {
            CloseAnimalInfo();
        }
    }

    private void RemoveAnimal(
        ARTrackedImage trackedImage)
    {
        if (!spawnedAnimals.TryGetValue(
                trackedImage.trackableId,
                out GameObject animal))
        {
            return;
        }

        spawnedAnimals.Remove(
            trackedImage.trackableId
        );

        spawnedConfigs.Remove(
            trackedImage.trackableId
        );

        if (CurrentAnimal == animal)
        {
            ClearCurrentAnimal();
        }

        Destroy(animal);
    }

    private void SetCurrentAnimal(
        GameObject animal,
        AnimalMarkerConfig config)
    {
        CurrentAnimal = animal;
        currentAnimalConfig = config;
        currentSound = config != null
            ? config.animalSound
            : null;

        Variables.Scene(gameObject.scene).Set(
            "modelObject",
            animal
        );
    }

    private void ClearCurrentAnimal()
    {
        CurrentAnimal = null;
        currentSound = null;
        currentAnimalConfig = null;

        Variables.Scene(gameObject.scene).Set(
            "modelObject",
            (GameObject)null
        );

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        CloseAnimalInfo();
    }

    public void PlayCurrentAnimalSound()
    {
        if (CurrentAnimal == null)
        {
            Debug.LogWarning(
                "Não existe um animal ativo no momento."
            );
            return;
        }

        if (currentSound == null)
        {
            Debug.LogWarning(
                "O animal atual não possui som configurado."
            );
            return;
        }

        audioSource.Stop();
        audioSource.PlayOneShot(currentSound);
    }

    public void OpenAnimalInfo()
    {
        if (CurrentAnimal == null || currentAnimalConfig == null)
        {
            Debug.LogWarning(
                "Não existe um animal ativo para mostrar informações."
            );
            return;
        }

        if (animalInfoPanel == null)
        {
            Debug.LogError(
                "AnimalInfoPanel não foi configurado no Inspector."
            );
            return;
        }

        SetText(
            animalNameText,
            currentAnimalConfig.displayName
        );

        SetText(
            scientificNameText,
            currentAnimalConfig.scientificName
        );

        SetText(
            biomeText,
            currentAnimalConfig.biome
        );

        SetText(
            dietText,
            currentAnimalConfig.diet
        );

        SetText(
            riskText,
            currentAnimalConfig.risk
        );

        animalInfoPanel.SetActive(true);
    }

    public void CloseAnimalInfo()
    {
        if (animalInfoPanel != null)
        {
            animalInfoPanel.SetActive(false);
        }
    }

    private void SetText(
        TMP_Text textComponent,
        string value)
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.text =
            string.IsNullOrWhiteSpace(value)
                ? "Não informado"
                : value;
    }
}
