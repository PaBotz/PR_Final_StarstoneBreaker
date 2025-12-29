using System.Collections.Generic;
using TMPro;
using UnityEngine;


//Gestion de HUD/Menus

// UIManager con soporte de red - FASE 2
//
// CAMBIOS RESPECTO A FASE 1:
// LÍNEA 16-17: Agregar referencias para múltiples jugadores
// LÍNEA 45: ActualizarPuntaje() → ActualizarPuntajesMultijugador()
// LÍNEA 68: MostrarFinDelJuego() → MostrarRanking()
// LÍNEA 82: Metodo nuevo CrearTextoJugador()
public class SCR_UIManager : MonoBehaviour
{
    public static SCR_UIManager Instancia { get; private set; }

    [Header("Referencias UI - SinglePlayer")]
    [SerializeField] private TextMeshProUGUI texto_Puntaje; //Mantener para compatibilidad ELIMINAR?
    [SerializeField] private TextMeshProUGUI texto_Temporizador;

    [Header("Referencias UI - Multiplayer FASE 2")]
    [SerializeField] private Transform contenedor_PuntajesJugadores; // Panel donde irán los textos de cada jugador
    [SerializeField] private GameObject prefab_TextoJugador; // Prefab con TextMeshPro para cada jugador


    [Header("Game Over")]
    [SerializeField] private GameObject panel_FinDelJuego;
    [SerializeField] private TextMeshProUGUI texto_PuntajeFinal; // Mantener para compatibilidad ELIMINAR?
    [SerializeField] private Transform panel_Ranking; // Panel donde irá el ranking
    [SerializeField] private GameObject prefab_TextoRanking; // Prefab para cada línea del ranking


    // Diccionario para guardar referencias a los textos de cada jugador
    private Dictionary<ulong, TextMeshProUGUI> textosPorJugador = new Dictionary<ulong, TextMeshProUGUI>();

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

    //OBSOLETO -> Mejor usar ActualizarPuntajesMultijugador(Dictionary<ulong, int> puntajes)
    /*  public void ActualizarPuntaje(int puntaje)
      {
          texto_Puntaje.text = $"Puntaje: {puntaje}";  
      } */


    // NUEVO FASE 2: Actualizar puntajes de múltiples jugadores
    public void ActualizarPuntajesMultijugador(Dictionary<ulong, int> puntajes)
    {
        // Para cada jugador en el diccionario
        foreach (var jugador in puntajes)
        {
            ulong clientId = jugador.Key;
            int puntaje = jugador.Value;

            // Si ya existe el texto de este jugador, actualizarlo
            if (textosPorJugador.ContainsKey(clientId))
            {
                textosPorJugador[clientId].text = $"Jugador {clientId}: {puntaje}";
            }
            else
            {
                // Si no existe, crear uno nuevo
                CrearTextoJugador(clientId, puntaje);
            }
        }

        // Opcional: Remover textos de jugadores desconectados
        List<ulong> jugadoresARemover = new List<ulong>();
        foreach (var textoJugador in textosPorJugador)
        {
            if (!puntajes.ContainsKey(textoJugador.Key))
            {
                jugadoresARemover.Add(textoJugador.Key);
            }
        }

        foreach (ulong clientId in jugadoresARemover)
        {
            if (textosPorJugador[clientId] != null)
            {
                Destroy(textosPorJugador[clientId].gameObject);
            }
            textosPorJugador.Remove(clientId);
        }
    }


    // NUEVO: Crear texto de puntaje para un jugador
    void CrearTextoJugador(ulong clientId, int puntaje) // Creo que no se usa, solo es por si algo. Ya lo veremos
    {
        // Si tenemos prefab, usarlo
        if (prefab_TextoJugador != null && contenedor_PuntajesJugadores != null)
        {
            GameObject nuevoTexto = Instantiate(prefab_TextoJugador, contenedor_PuntajesJugadores);
            TextMeshProUGUI tmp = nuevoTexto.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                tmp.text = $"Jugador {clientId}: {puntaje}";
                textosPorJugador[clientId] = tmp;
            }
        }
        // Si no hay prefab, usar el texto único (fallback)
        else if (texto_Puntaje != null && textosPorJugador.Count == 0)
        {
            texto_Puntaje.text = $"Jugador {clientId}: {puntaje}";
            textosPorJugador[clientId] = texto_Puntaje;
        }
    }


    public void ActualizarTimer(float tiempoRestante)
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60);

        texto_Temporizador.text = $"Tiempo: {minutos:00}:{segundos:00}";
    }

    //OBSOLETO.Mejor usar  MostrarRanking
    /*public void MostrarFinDelJuego(int puntajeFinal)
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
    }*/

    // NUEVO: Mostrar ranking de múltiples jugadores

    public void MostrarRanking(List<KeyValuePair<ulong, int>> ranking)
    {
        panel_FinDelJuego.SetActive(true);

        // Limpiar ranking anterior
        if (panel_Ranking != null)
        {
            foreach (Transform child in panel_Ranking)
            {
                Destroy(child.gameObject);
            }
        }

        // Mostrar título
        if (texto_PuntajeFinal != null)
        {
            texto_PuntajeFinal.text = "RANKING FINAL";
        }

        // Crear línea de ranking para cada jugador
        for (int i = 0; i < ranking.Count; i++)
        {
            ulong clientId = ranking[i].Key;
            int puntaje = ranking[i].Value;
            string medalla = ObtenerMedalla(i);

            CrearLineaRanking(i + 1, clientId, puntaje, medalla);
        }

        Debug.Log("=== RANKING MOSTRADO ===");
    }

    // NUEVO: Crear una línea del ranking
    void CrearLineaRanking(int posicion, ulong clientId, int puntaje, string medalla)
    {
        // Si tenemos prefab y contenedor, crear texto
        if (prefab_TextoRanking != null && panel_Ranking != null)
        {
            GameObject lineaObj = Instantiate(prefab_TextoRanking, panel_Ranking);
            TextMeshProUGUI tmp = lineaObj.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                tmp.text = $"{medalla} #{posicion} - Jugador {clientId}: {puntaje} puntos";

                // Colorear según posición
                tmp.color = posicion switch
                {
                    1 => Color.yellow,   // Oro
                    2 => new Color(0.75f, 0.75f, 0.75f), // Plata
                    3 => new Color(0.8f, 0.5f, 0.2f),    // Bronce
                    _ => Color.white
                };
            }
        }
        // Fallback: Usar texto único
        else if (texto_PuntajeFinal != null)
        {
            texto_PuntajeFinal.text += $"\n{medalla} Jugador {clientId}: {puntaje} puntos";
        }
    }


    // NUEVO: Obtener emoji de medalla según posición
    string ObtenerMedalla(int indice)
    {
        return indice switch
        {
            0 => "🥇",
            1 => "🥈",
            2 => "🥉",
            _ => "  "
        };
    }


    public void Activar_BotonRestar()
    {
        if (panel_FinDelJuego != null)
        {
            panel_FinDelJuego.SetActive(false);
        }

        // NUEVO
        // Limpiar textos de jugadores
        foreach (var texto in textosPorJugador.Values)
        {
            if (texto != null && texto != texto_Puntaje)
            {
                Destroy(texto.gameObject); //Destruyo objetos
            }
        }
        textosPorJugador.Clear(); // Vaciar todo el dictionary

        SCR_GameManager.Instancia?.RestarJuego();
    }

}
