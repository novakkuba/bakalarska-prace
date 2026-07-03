using UnityEngine;
using System.Collections;


[RequireComponent(typeof(AudioSource))]
public class MusicCorsiBlock : MonoBehaviour
{
    [HideInInspector]
    public int ID;

    [Header("Vizuální komponenty")]
    private Renderer _renderer;
    private Color _originalColor;

    
    private ICorsiLevelManager _manager;

    [Header("Generátor zvuku")]
    private float sampleRate = 48000f;
    private double phase = 0.0;
    private double increment = 0.0;
    private bool isPlaying = false;
    private float volume = 0f;
    private float baseFrequency = 440f;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;

        // Nastavení 3D zvuku
        AudioSource source = GetComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.playOnAwake = false;
        sampleRate = AudioSettings.outputSampleRate;
    }

    
    public void SetupBlock(ICorsiLevelManager levelManager, int blockID)
    {
        _manager = levelManager;
        ID = blockID;
    }

    public void SetupTone(float pitchMultiplier)
    {
        baseFrequency = 440f * pitchMultiplier;
    }

    public void Highlight(Color color, float duration)
    {
        StartCoroutine(HighlightRoutine(color, duration));

        // Spustí generátor zvuku
        volume = 0.8f;
        isPlaying = true;
    }

    private IEnumerator HighlightRoutine(Color color, float duration)
    {
        if (_renderer != null) _renderer.material.color = color;
        yield return new WaitForSeconds(duration);
        if (_renderer != null) _renderer.material.color = _originalColor;
    }

    public void Zmacknuto()
    {
        // Rychlý vizuální feedback po kliknutí
        Highlight(Color.cyan, 0.15f);

        
        if (_manager != null)
        {
            _manager.PlayerSelectedBlock(ID);
        }
    }

    // Pojistka pro Meta XR SDK (Poke Interactable)
    public void OnPoke()
    {
        Zmacknuto();
    }

    // TVÙJ FUNKÈNÍ AUDIO SYNTEZÁTOR
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isPlaying) return;

        increment = baseFrequency * 2.0 * Mathf.PI / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            float value = Mathf.Sin((float)phase) * volume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = value;
            }

            volume -= 2.5f / sampleRate;

            if (volume <= 0f)
            {
                volume = 0f;
                isPlaying = false;
            }

            if (phase > 2 * Mathf.PI) phase -= 2 * Mathf.PI;
        }
    }
}