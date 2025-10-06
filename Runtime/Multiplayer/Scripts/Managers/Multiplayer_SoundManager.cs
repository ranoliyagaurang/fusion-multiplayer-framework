using UnityEngine;

public class Multiplayer_SoundManager : MonoBehaviour
{
    public static Multiplayer_SoundManager Instance;

    [Header("General")]
    [SerializeField] AudioSource click;
    [SerializeField] float sound = 0.5f;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayClick()
    {
        click.volume = sound;
        click.Play();
    }
}