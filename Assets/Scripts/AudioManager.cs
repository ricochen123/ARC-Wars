using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("-----Audio Source-----")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("-----Music-----")]
    public AudioClip title;
    public AudioClip brody;
    public AudioClip emry;
    public AudioClip four;
    public AudioClip rava;

    [Header("-----SFX-----")]
    public AudioClip menuClick;
    public AudioClip hit;

    public static AudioManager instance;

    //this awake is needed since we're gonna PlayMusic in game controller
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.clip = title;
        musicSource.Play();
        sfxSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    private void Update()
    {

    }

    public void PlayMusic(int id) //Play music based on current commander
    {
        switch (id)
        {
            case 1:
                musicSource.clip = brody;
                musicSource.Play();
                break;
            case 2:
                musicSource.clip = emry;
                musicSource.Play();
                break;
            case 3:
                musicSource.clip = four;
                musicSource.Play();
                break;
            case 4:
                musicSource.clip = rava;
                musicSource.Play();
                break;
            default:
                break;
        }
    }

    public void PlayHitSound()
    {
        sfxSource.PlayOneShot(hit);
    }
}
