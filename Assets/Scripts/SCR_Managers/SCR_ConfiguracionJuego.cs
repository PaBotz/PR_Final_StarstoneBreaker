using UnityEngine;

//Creacion de una Instancia publica y accesible desde cualquier parte del codigo,
//ademas, todas las variables estan en public, lo que facilita el uso contante de las mismas
public class SCR_ConfiguracionJuego : MonoBehaviour
{
    public static SCR_ConfiguracionJuego Instancia { get; private set; }  //Creacion de una Instancia publica y accesible desde cualquier parte del codigo, ademas, todas las variables estan en public, lo que facilita el uso contante de las mismas

    [Header("Tiempo de Partida")]
    public float duracionDeMatch = 120f;

    [Header("Límites del Escenario")]
    public float minX = 1f;
    public float maxX = 1f;
    public float minY = 0f;
    public float maxY = 10f;

    [Header("Configuración del Jugador")]
    public float velocidad_Jugador = 8f;
    public float cadencia_Disparo = 0.2f; 
    public float duracion_Aturdimiento = 1f; 

    [Header("Configuración de Balas")]
    public float velocidad_Bala = 15f;
    public float bala_Lifetime = 3f;

    [Header("Configuración de Asteroides")]
    public float velocidad_Meteorito_L = 1f;
    public float velocidad_Meteorito_M = 2f;
    public float velocidad_Meteorito_S = 4f;
    public int fragmentosPorMeteorito = 3; // Cuantos fragmentos genera un asteroide grande al explotar
    public float intervalo_MeteoritoSpawn = 3f;


    [Header("Salud de Asteroides")]
    public int vida_Meteorito_L = 5;
    public int vida_Meteorito_M = 3;
    public int vida_Meteorito_S = 1;

    [Header("Sistema de Puntos")]
    public int puntosPorMeteorito_L = 15;
    public int puntosPorMeteorito_M = 10;
    public int puntosPorMeteorito_S = 5;
    public int puntosPorBonificacion = 20;
    public int golpe_Penalizacion = -5;

    [Header("Arrojar Bonificacion")]
    [Range(0f, 1f)] public float ProbabilidadDeBonificacion = 0.3f; // 30% de probabilidad

    [Header("Bonificaciones")]
    public float duracion_PowerUp = 5f;
    public float disparoBoostMultiplicador = 2f; // Multiplica la velocidad de disparo
    public float velocidadBoost_Multiplicador = 1.5f;  // Multiplica la velocidad de movimiento

    [Header("Floating Text (Game Feel)")]
    public float textoFlotante_LifeTime = 0.5f;
    public float teextoFlotante_VelocidadMovimiento = 2f;
    public float textoFlotante_VelocidadFade = 2f;

    private void Awake()
    {
        if(Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


}
