using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.STP;

[RequireComponent(typeof(Rigidbody2D))]
public class SCR_PlayerController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private Transform puntoDeDisparo;

    [Header("Configuracion")]
    [SerializeField] private KeyCode teclaDisparo = KeyCode.Space;

    private Rigidbody2D rb;
    private SCR_ConfiguracionJuego configuracion;

    private float movimientoLateral;
    private float siguienteDisparo;
    private bool estaAturdido;
    private float velocidad_JugadorActual;
    private float cadenciaDeDisparoActual;
    private bool tieneEscudo;

    public bool TieneEscudo => tieneEscudo; //Getter simple; Solo lectura

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;
        velocidad_JugadorActual = configuracion.velocidad_Jugador;
        cadenciaDeDisparoActual = configuracion.cadencia_Disparo;
    }


    void Update()
    {
        if (!estaAturdido && Input.GetKey(teclaDisparo))
        {
            ControladorDisparo();
        }

    }

    private void FixedUpdate()
    {
        if (!estaAturdido)
        {
            ControladorMovimiento();
        }
                                                   
    }

    #region Movimiento con nuevo Input Handle
    void ControladorMovimiento()
    {

        float targetX = transform.position.x + movimientoLateral * velocidad_JugadorActual * Time.fixedDeltaTime;
        targetX = Mathf.Clamp(targetX, configuracion.minX, configuracion.maxX);

        rb.MovePosition(new Vector2(targetX, rb.position.y));

    }

    public void OnMove(InputValue value)  // PREGUNTA PARA GEPETO ¿En donde se llama está funcion. En el sistema de inputs?
    {
        movimientoLateral = value.Get<Vector2>().x;
    }

    #endregion

    //Cadencia de disparo
    void ControladorDisparo()
    {
        if(Time.time >= siguienteDisparo)
        {
            Disparo();
            siguienteDisparo = Time.time + cadenciaDeDisparoActual;

        }
    }

    //Instanciar bala
  void Disparo()
    {
        if (balaPrefab != null && puntoDeDisparo != null)
        {
            Instantiate(balaPrefab, puntoDeDisparo.position, Quaternion.identity); 
        }
        else
        {
            Debug.LogWarning("Papi!! Se te olvido referenciar la balaPrefab y el puntoDeDisparo. Ponete las pilas");
        }
    }

    public void AplicarAturdimiento()
    {
        if (!estaAturdido)
        {
            estaAturdido = true;
            Invoke(nameof(RemoverAturdimiento), configuracion.duracion_Aturdimiento); // Invoca el metodo, tras "x" segundos
        }
    }

    void RemoverAturdimiento()
    {
        estaAturdido = false;
    }


    public void AplicarBoostDeDisparo()
    {
        cadenciaDeDisparoActual = configuracion.cadencia_Disparo / configuracion.disparoBoostMultiplicador;
        Invoke(nameof(RestarCadenciaDeDisparo), configuracion.duracion_PowerUp);
    }


    public void RestarCadenciaDeDisparo()
    {
        cadenciaDeDisparoActual = configuracion.cadencia_Disparo;
    }

   public void AplicarBoost_Velocidad()
    {
        velocidad_JugadorActual = configuracion.velocidad_Jugador * configuracion.velocidadBoost_Multiplicador;
        Invoke(nameof(RestarVelocidad), configuracion.duracion_PowerUp); 
    }

    void RestarVelocidad()
    {
        velocidad_JugadorActual = configuracion.velocidad_Jugador;
    }

    public void AplicarEscudo()
    {
        tieneEscudo = true;
        Debug.Log("TieneEscudo");

        //Agregar mas codigo que muestr visualmente el escudo, puede ser con los sprites. 
        //Si es necesario, podria agregarse 2 funciones nuevas que se activen dependiendo de si tiene o no tiene el escudo.
    }

   

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Colisiono con meteorito");
        if (collision.gameObject.CompareTag("Meteorito"))
        {
            if (tieneEscudo)
            {
                tieneEscudo = false;
                SCR_TextoFlotanteManager.Instancia.MostrarBonificacion("EscudoRoto!", transform.position);
                Debug.Log("Escudo gastado");

            }
            else 
            {
                AplicarAturdimiento();
                SCR_GameManager.Instancia.SumarPuntos(configuracion.golpe_Penalizacion);
                SCR_TextoFlotanteManager.Instancia.MostrarPuntaje(configuracion.golpe_Penalizacion, transform.position);
            }
        }
    }

}
