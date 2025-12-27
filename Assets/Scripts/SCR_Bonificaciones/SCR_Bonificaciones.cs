using UnityEngine;
using static UnityEngine.Rendering.STP;

//PowerUps
public class SCR_Bonificaciones : MonoBehaviour
{
    public enum TipoDeBonificacion
    {
        DiparoBoost,
        PuntosInstantaneos,
        BurbujaProtectora,
        MovimientoBoost
    }

    [SerializeField] private TipoDeBonificacion bonificacion;
    [SerializeField] private float velocidadRotacion = 50f;

    private SCR_ConfiguracionJuego configuracion;

    void Start()
    {
        configuracion = SCR_ConfiguracionJuego.Instancia;
    }


    void Update()
    {
        // Rotación visual de la bonificacion
        transform.Rotate(Vector3.forward * velocidadRotacion * Time.deltaTime);

        //Destruir el bonus si sale de la vision de la camara
        if (transform.position.y < configuracion.minY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarEfecto(other.gameObject);
            Destroy(gameObject);
        }

    }

    void AplicarEfecto(GameObject jugador)
    {
        SCR_PlayerController playercontroler = jugador.GetComponent<SCR_PlayerController>();

        SCR_PlayerController control = jugador.GetComponent<SCR_PlayerController>(); //Ahora en el singler player esto es opcional, pero en el modo "multiplayer", esta linea nos ayudara a encontrar "el player" que ha golpeado el powerUp
        if (control == null) return;

        string textoBonificacion = "";

        switch (bonificacion)
        {
            case TipoDeBonificacion.DiparoBoost:
                control.AplicarBoostDeDisparo();
                textoBonificacion = "¡Boost Disparo!";
                break;

            case TipoDeBonificacion.PuntosInstantaneos:
                SCR_GameManager.Instancia.SumarPuntos(configuracion.puntosPorBonificacion); // esta despues tendra que cambiarse por una instancia referenciada al puntaje del player que ha cogido el powerup
                textoBonificacion = $"+{configuracion.puntosPorBonificacion} Points!";
                break;

            case TipoDeBonificacion.BurbujaProtectora:
                playercontroler.AplicarEscudo();
                textoBonificacion = "¡Escudo!";
                break;

            case TipoDeBonificacion.MovimientoBoost:
                control.AplicarBoost_Velocidad();
                textoBonificacion = "¡Boost Velocidad!";
                break;

        }

        SCR_TextoFlotanteManager.Instancia.MostrarBonificacion(textoBonificacion, transform.position);

    }

   
}
