using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))] 
public class SCR_textoFlotante : MonoBehaviour
{
    [Header("Configuración de Animación")]
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private float velocidadMovimiento = 2f;
    [SerializeField] private float velocidadFade = 2f;

    private TextMeshPro textMesh; 
    private Color originalColor;
    private float spawnTime;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        originalColor = textMesh.color;
        spawnTime = Time.time;
    }

    void Update()
    {
        // Mover hacia arriba
        transform.position += Vector3.up * velocidadMovimiento * Time.deltaTime;

        // Fade out progresivo
        float timerVida = Time.time - spawnTime;
        float alpha = Mathf.Lerp(1f, 0f, timerVida / lifetime);
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        // Auto-destruirse después del lifetime
        if (timerVida >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void Ajuste(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
        originalColor = color;
    }

    public void Ajuste(string text)
    {
        textMesh.text = text;
        originalColor = textMesh.color;
    }
}