using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    private static BackgroundMusic _instance;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AudioSource src = GetComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.Play();
    }
}