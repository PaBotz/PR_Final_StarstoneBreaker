using UnityEngine;
using Unity.Netcode;

//PowerUps
// Power-Ups con soporte de red - FASE 2
// 
// CAMBIOS RESPECTO A FASE 1:
// LÍNEA 5: MonoBehaviour → NetworkBehaviour
// LÍNEA 29-36: Update() con check de servidor
// LÍNEA 40: OnTriggerEnter2D con check de servidor
// LÍNEA 48: AplicarEfecto ahora obtiene clientId
// LÍNEA 77: MostrarBonificacion → MostrarBonificacionRpc (todos lo ven)
// LÍNEA 45: Destroy → DestruirPowerUp() con NetworkDespawn
public class SCR_Bonificaciones : NetworkBehaviour
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
        if (!IsServer) return;
        // Rotación visual de la bonificacion
        transform.Rotate(Vector3.forward * velocidadRotacion * Time.deltaTime); // NOTA: NetworkTransform sincroniza la rotación automáticamente

        //Destruir el bonus si sale de la vision de la camara
        if (transform.position.y < configuracion.minY)
        {
            //Destroy(gameObject); Fase 1
            DestruirBonificador();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!IsServer) return;
        if (other.CompareTag("Player"))
        {
            // CAMBIO 4: Obtener el clientId del jugador que recogió el power-up
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
            ulong clienteId = playerNetObj.OwnerClientId;

            AplicarEfecto(other.gameObject, clienteId);

            DestruirBonificador();
        }
    }


    //void AplicarEfecto(GameObject jugador) //Fase 1
    void AplicarEfecto(GameObject jugador, ulong clienteID) //siempre que se necesite saber a que jugador se le aplica la funcion, utilizamos ulong clienteID
    {
        SCR_PlayerController playercontroler = jugador.GetComponent<SCR_PlayerController>();

        SCR_PlayerController control = jugador.GetComponent<SCR_PlayerController>(); //Ahora en el singler player esto es opcional, pero en el modo "multiplayer", esta linea nos ayudara a encontrar "el player" que ha golpeado el powerUp
        if (control == null) return;

        string textoBonificacion = "";

        switch (bonificacion)
        {
            case TipoDeBonificacion.DiparoBoost:
                control.AplicarBoostDeDisparoRpc();
                textoBonificacion = "¡Boost Disparo!";
                break;

            case TipoDeBonificacion.PuntosInstantaneos:
                //SCR_GameManager.Instancia.SumarPuntos(configuracion.puntosPorBonificacion); //Fase 1: esta despues tendra que cambiarse por una instancia referenciada al puntaje del player que ha cogido el powerup
                SCR_GameManager.Instancia.SumarPuntosServerRpc(clienteID, configuracion.puntosPorBonificacion);
                textoBonificacion = $"+{configuracion.puntosPorBonificacion} Points!";
                break;

            case TipoDeBonificacion.BurbujaProtectora:
                playercontroler.AplicarEscudoSeverRpc();
                textoBonificacion = "¡Escudo!";
                break;

            case TipoDeBonificacion.MovimientoBoost:
                control.AplicarBoost_VelocidadRpc();
                textoBonificacion = "¡Boost Velocidad!";
                break;

        }

        // CAMBIO 7: Mostrar texto flotante a TODOS los jugadores
        // ANTES: SCR_TextoFlotanteManager.Instancia.MostrarBonificacion(textoBonificacion, transform.position);
        // AHORA:
        MostrarBonificacionRpc(textoBonificacion, transform.position);

    }

    // NUEVO: RPC para que todos los jugadores vean el floating text
    [Rpc(SendTo.Everyone)] 
    void MostrarBonificacionRpc(string texto, Vector3 posicion)
    {
        if (SCR_TextoFlotanteManager.Instancia != null)
        {
            SCR_TextoFlotanteManager.Instancia.MostrarBonificacion(texto, posicion);
        }
    }

    // NUEVO: Destruir power-up de forma sincronizada
    void DestruirBonificador()
    {
        if (!IsServer) return;

        if (NetworkObject != null)
        {
            // CRÍTICO: Despawn sincronizado para que todos vean la destrucción
            NetworkObject.Despawn();
        }

        Destroy(gameObject);
    }

}
