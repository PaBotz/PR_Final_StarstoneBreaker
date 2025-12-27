using TMPro;
using UnityEngine;


//Gestion de HUD/Menus
public class SCR_UIManager : MonoBehaviour
{
    public static SCR_UIManager Instancia { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI texto_Puntaje;
    [SerializeField] private TextMeshProUGUI texto_Temporizador;
    [SerializeField] private GameObject panel_FinDelJuego;
    [SerializeField] private TextMeshProUGUI texto_PuntajeFinal;


    private void Awake()
    {
        if(Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    void Start()
    {
        panel_FinDelJuego.SetActive(false);
    }


    public void ActualizarPuntaje(int puntaje)
    {
        texto_Puntaje.text = $"Puntaje: {puntaje}";  
    }

    public void ActualizarTimer(float tiempoRestante)
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60);

        texto_Temporizador.text = $"Tiempo: {minutos:00}:{segundos:00}";
    }

    public void MostrarFinDelJuego(int puntajeFinal)
    {
        panel_FinDelJuego.SetActive(true);

        texto_PuntajeFinal.text = $"Puntaje Final: {puntajeFinal}";

        if(panel_FinDelJuego == null)
        {
            Debug.Log("panel_FinDelJuego sin referencia.");
        }
        if (texto_PuntajeFinal == null)
        {
            Debug.Log("texto_PuntajeFinal sin referencia.");
        }


    }

    public void Activar_BotonRestar()
    {
        if (panel_FinDelJuego != null)
        {
            panel_FinDelJuego.SetActive(false);
        }

        SCR_GameManager.Instancia?.RestarJuego();
    }

}
