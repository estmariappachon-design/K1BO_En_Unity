using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicaManager : MonoBehaviour
{
    [Header("Configuración de Música")]
    public AudioClip Ambiente_K1B0;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Configurar el AudioSource
        audioSource.clip = Ambiente_K1B0;
        audioSource.loop = true;        // Se repite 
        audioSource.playOnAwake = false;
        audioSource.volume = KiboElOrigen.KiboAjustes.VolumenMusica; // volumen de Ajustes
    }

    void OnEnable()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play(); // Suena al activarse/correr
    }

    void OnDisable()
    {
        audioSource.Stop(); // Para al desactivarse/dejar de correr
    }


    public void PausarMusica()
    {
        audioSource.Pause();
    }

    public void ReanudarMusica()
    {
        audioSource.UnPause();
    }

}
