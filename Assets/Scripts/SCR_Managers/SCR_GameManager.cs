using TMPro.EditorUtilities;
using UnityEngine;

//Gestion de estados , tiempo, puntaje, spawn de meteoritos
public class SCR_GameManager : MonoBehaviour
{
    public static SCR_GameManager Instancia { get; private set; } //Los demas lo pueden leer, pero no modificar

    [Header("Referencias")]
    [SerializeField] private GameObject prefab_Meteorito_L;
    [SerializeField] private Transform[] PuntosDeSpawn;

    private SCR_ConfiguracionJuego configuracion;
    private int puntajeActual;
    private float tiempoRestante;
    private bool juegoActivo;
    private float siguienteMeteorito_Spawn;

    public int PuntajeActual => puntajeActual;         // "x => z" Uso de operador lambda como getter, es lo mismo que hacer esto:   
    public float TiempoRestante => tiempoRestante;        /// public int CurrentScoreb
    public bool JuegoActivo => juegoActivo;             ///  {
                                                         ///    get { return currentScore; }
                                                         ///  }  
                                                      // Se usa para exponer datos a lectura, pero evita que se puedan modificar.

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
        configuracion = SCR_ConfiguracionJuego.Instancia;
        EmpezarJuego();
    }

    void Update()
    {
        if (!JuegoActivo) return;
        ActualizarTimer();
        Controlador_MeteoritosSpawn();
    }

    void EmpezarJuego()
    {
        puntajeActual = 0;
        tiempoRestante = configuracion.duracionDeMatch;
        juegoActivo = true;
        siguienteMeteorito_Spawn = Time.time + configuracion.intervalo_MeteoritoSpawn;

        SCR_UIManager.Instancia?.ActualizarPuntaje(puntajeActual);
        SCR_UIManager.Instancia?.ActualizarTimer(tiempoRestante);

    }

    void ActualizarTimer()
    {
        tiempoRestante -= Time.deltaTime;
        SCR_UIManager.Instancia?.ActualizarTimer(tiempoRestante);
        if(tiempoRestante <= 0)
        {
            FinalizarJuego();
        }
    }

    void Controlador_MeteoritosSpawn()
    {
        if(Time.time >= siguienteMeteorito_Spawn)
        {
            MeteoritoSpawn();
            siguienteMeteorito_Spawn = Time.time + configuracion.intervalo_MeteoritoSpawn;
        }
    }

    void MeteoritoSpawn()
    {
        Transform puntoDeSpawn = PuntosDeSpawn[Random.Range(0, PuntosDeSpawn.Length)];
        Instantiate(prefab_Meteorito_L,puntoDeSpawn.position, Quaternion.identity);
    }

    public void SumarPuntos(int puntos)
    {
        puntajeActual += puntos;
        puntajeActual = Mathf.Max(0,puntajeActual);    
        SCR_UIManager.Instancia?.ActualizarPuntaje(puntajeActual);
    }

    void FinalizarJuego()
    {
        juegoActivo = false;
        SCR_UIManager.Instancia?.MostrarFinDelJuego(puntajeActual);
        Debug.Log($"Juego Terminado. Putaje Final {puntajeActual}");

    }

    public void RestarJuego()
    {
        //Destruir todos los asteroides y balas para reiniciar el juego
        foreach(GameObject i in GameObject.FindGameObjectsWithTag("Meteorito"))
        {
            Destroy(i);
        }
        foreach (GameObject i in GameObject.FindGameObjectsWithTag("Bala"))
        {
            Destroy(i);
        }

        EmpezarJuego();
    }


}
