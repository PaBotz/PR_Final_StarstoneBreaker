
using UnityEngine;

public class SCR_TextoFlotanteManager : MonoBehaviour
{
    public static SCR_TextoFlotanteManager Instancia { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GameObject prefab_textoFlotante;

    [Header("Colores")]
    [SerializeField] private Color colorPuntos = Color.yellow;
    [SerializeField] private Color colorBonificacion = Color.cyan;
    [SerializeField] private Color colorPenalizacion = Color.red;

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnearTextoFlotante(string texto, Vector2 posicion, Color color)
    {
        if (prefab_textoFlotante == null)
        {
            Debug.LogWarning("prefab_textoFlotante es null");
            return;
        }

        // Instantiate directo en posición de mundo 
        GameObject objText = Instantiate(prefab_textoFlotante, posicion, Quaternion.identity);

        SCR_textoFlotante textoFlotante = objText.GetComponent<SCR_textoFlotante>();
        if (textoFlotante != null)
        {
            textoFlotante.Ajuste(texto, color);
        }
        else
        {
            Debug.LogWarning("No se encontró SCR_textoFlotante en el prefab");
        }
    }

    public void MostrarPuntaje(int puntos, Vector2 posicion)
    {
        string prefijo = puntos > 0 ? "+" : "";
        SpawnearTextoFlotante($"{prefijo}{puntos}", posicion, puntos > 0 ? colorPuntos : colorPenalizacion);
    }

    public void MostrarBonificacion(string bonificacionNombre, Vector3 posicion)
    {
        SpawnearTextoFlotante(bonificacionNombre, posicion, colorBonificacion);
    }
}
