using Unity.Netcode;
using UnityEngine;

// CAMBIOS RESPECTO A FASE 1:
// MonoBehaviour → NetworkBehaviour
// int saludActual → NetworkVariable<int> saludActual
// Inicialización solo en servidor (IsServer check)
// Update() eliminado (NetworkRigidbody2D maneja física)
// RecibirDano() → RecibirDanoRpc(ulong clientId)
// SumarPuntos() → SumarPuntosServerRpc(clientId, puntos)
// MostrarPuntaje → MostrarPuntajeRpc (visible para todos)
// Destroy → DestruirMeteorito()
// Instantiate → NetworkSpawn (power-ups)
// Instantiate → NetworkSpawn (fragmentos)
[RequireComponent(typeof(Rigidbody2D))]
public class SCR_Meteorito : NetworkBehaviour
{
    public enum MeteoritoTamano { L, M, S }

    [SerializeField] private MeteoritoTamano tamano = MeteoritoTamano.L;
    [SerializeField] private GameObject prefab_Meteorito_M;
    [SerializeField] private GameObject prefab_Meteorito_S;
    [SerializeField] private GameObject[] bonificadores_Prefabs; // Array de prefabs de power-ups

    private Rigidbody2D rb;
    private SCR_ConfiguracionJuego configuracion;
    private Vector2 direccion;

    private NetworkVariable<int> saludActual = new NetworkVariable<int>();  //Fase 2

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;

       if (IsServer){
            IniciarMovimiento();
            IniciarVida();
        }
    }


    // CAMBIO 4: Update() ELIMINADO
    // RAZÓN: NetworkRigidbody2D sincroniza la física automáticamente
    // El rebote contra paredes ahora se maneja en OnCollisionEnter2D
    // Los límites se aplican mediante Colliders en los bordes del mapa
    private void Update()
    {
        if (!IsServer) return;
        RevisarLimites();
    }


    void IniciarVida()
    {
        switch (tamano)
        {
            case MeteoritoTamano.L:
                saludActual.Value = configuracion.vida_Meteorito_L;
                break;
            case MeteoritoTamano.M:
                saludActual.Value = configuracion.vida_Meteorito_M;
                break;
            case MeteoritoTamano.S:
                saludActual.Value = configuracion.vida_Meteorito_S;
                break;
        }
    }

    void IniciarMovimiento()
    {
        direccion = new Vector2(Random.Range(-1, 1f), Random.Range(-1f, 1f)).normalized;

        float velocidad;
        switch (tamano)
        {
            case MeteoritoTamano.L:
                velocidad = configuracion.velocidad_Meteorito_L;
                break;
            case MeteoritoTamano.M:
                velocidad = configuracion.velocidad_Meteorito_M;
                break;
            case MeteoritoTamano.S:
                velocidad = configuracion.velocidad_Meteorito_S;
                break;
            default:
                velocidad = 0;
                break;
        }
        rb.linearVelocity = direccion * velocidad;
    }

    void RevisarLimites()
    {
        Vector2 posicion = rb.position;
        bool Reboto = false;

        if (posicion.x <= configuracion.minX || posicion.x >= configuracion.maxX)
        {
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y); // invierte la direccion de X, deja Y como esta
            Reboto = true;
        }

        if (posicion.y <= configuracion.minY || posicion.y >= configuracion.maxY)
        {
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, -rb.linearVelocity.y); // Viceversa
            Reboto = true;
        }

        //Comprobacion.
        if (Reboto)
        {
            posicion.x = Mathf.Clamp(posicion.x, configuracion.minX, configuracion.maxX); //clamp: Obliga al gameobject a quedarse en esos margenes. Auque en este codigo sirve más como una comprobacion que otra cosa,
            posicion.y = Mathf.Clamp(posicion.y, configuracion.minY, configuracion.maxY); //pues ya se tiene los limites de arriba, aun asi es muy necesario, pues dependiendo de los fps podria traspasar los limites sin que unity pueda evitarlo.
            rb.position = posicion;
        }

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    //public void RecibirDano() //Fase 1
    public void RecibirDanoRpc(ulong clientId) //Fase 2
    {
        saludActual.Value --;
        if(saludActual.Value <= 0)
        {
            int puntos = ObtenerPuntos_PorTamano();

            //Probabilidad de que salga un power up
            LanzarDadoPowerUp();

            //Suma Puntos
            MostrarPuntajeRpc(puntos, transform.position); // SCR_GameManager.Instancia.SumarPuntosServerRpc(clientId, puntos); CAMBIALO cuando se modifique el gameManager

            //Dividir el meteoro en meteoritos
            SpawnFragmentos();

            //Puntos flotantesManager
            SCR_TextoFlotanteManager.Instancia.MostrarPuntaje(puntos, transform.position);

            // CAMBIO 10: Destrucción sincronizada
            // ANTES: Destroy(gameObject);
            // AHORA:
            DestruirMeteorito();

        }
       
    }

    // NUEVO METODO
    // Mostrar puntos visible para todos los clientes
    //Cambia 

    [Rpc(SendTo.Everyone)]
    void MostrarPuntajeRpc(int puntos, Vector3 posicion)
    {
        if (SCR_TextoFlotanteManager.Instancia != null)
        {
            SCR_TextoFlotanteManager.Instancia.MostrarPuntaje(puntos, posicion);
        }
    }

    void LanzarDadoPowerUp()
    {
        if (!IsServer) return; //Fase 2

        if (bonificadores_Prefabs.Length != 0)
        {
            if (Random.value < configuracion.ProbabilidadDeBonificacion)
            {
                GameObject bonificacionAleatoria = bonificadores_Prefabs[Random.Range(0, bonificadores_Prefabs.Length)];
                // CAMBIO 11: Instantiate + NetworkSpawn
                // ANTES: Instantiate(bonificacionAleatoria, transform.position, Quaternion.identity);
                GameObject bonificacion = Instantiate(bonificacionAleatoria, transform.position, Quaternion.identity);

                NetworkObject BoniticacionNetObj = bonificacion.GetComponent<NetworkObject>();
                if (BoniticacionNetObj == null) Debug.LogWarning("Bonificadores sin networkObject");
                BoniticacionNetObj.Spawn();
            }
        }
    }

    int ObtenerPuntos_PorTamano() 
    {
        switch (tamano)
        {
            case MeteoritoTamano.L:
                return configuracion.puntosPorMeteorito_L;
            case MeteoritoTamano.M:
                return configuracion.puntosPorMeteorito_M;
            case MeteoritoTamano.S:
                return configuracion.puntosPorMeteorito_S;
            default:
                return 0;
        }
    }

    void SpawnFragmentos()
    {
        if (!IsServer) return;
        GameObject fragmentoPrefab = null;
        MeteoritoTamano nuevoTamano = tamano; //El nuevotamano es igual a tamano

        // Determinar qué prefab usar según el tamaño actual
        switch (tamano)
        {
            case MeteoritoTamano.L:
                fragmentoPrefab = prefab_Meteorito_M;
                nuevoTamano = MeteoritoTamano.M;
                break;
            case MeteoritoTamano.M:
                fragmentoPrefab = prefab_Meteorito_S;
                nuevoTamano = MeteoritoTamano.S;
                    break;
            case MeteoritoTamano.S:
                // Los asteroides pequeños no generan fragmentos
                return;
        }

        if (fragmentoPrefab == null) return;

        // CAMBIO 12: Spawear fragmentos con NetworkSpawn
        for (int i = 0; i < configuracion.fragmentosPorMeteorito; i++)
        {
            // ANTES: GameObject fragmento = Instantiate(fragmentoPrefab, transform.position, Quaternion.identity); CAMBIO INECESARIO PARA GAMEOBJECT, pero si que es una mejora
            // AHORA:
            Vector2 offset = Random.insideUnitCircle * 0.5f; //Agregar el offset no es un cambio para el multiplayer, es un cambio para evitar errores de asolapamiento cuando los meteoritos spawneen
            GameObject fragmento = Instantiate(fragmentoPrefab, (Vector2)transform.position + offset, Quaternion.identity);

            // Configurar el tamaño del fragmento
            SCR_Meteorito fragmentosMeteorito = fragmento.GetComponent<SCR_Meteorito>();
            if (fragmentosMeteorito != null)
            {
                    fragmentosMeteorito.tamano = nuevoTamano;
            }
            // CRÍTICO: Spawear en red // Fase 2. 
            NetworkObject fragmentoNetObj = fragmento.GetComponent<NetworkObject>();
            if (fragmentoNetObj != null)
            {
                fragmentoNetObj.Spawn(); // Notificacion a todos los clientes
            }
            else
            {
                Debug.LogWarning($"Fragmento {fragmento.name} no tiene NetworkObject!");
                Destroy(fragmento); // Limpiar si no tiene NetworkObject
            }
        }
    }


    // NUEVO: Destruir meteorito de forma sincronizada
    void DestruirMeteorito()
    {
        if (!IsServer) return;

        if (NetworkObject != null)
        {
            // Despawn sincronizado (todos los clientes ven la destrucción)
            NetworkObject.Despawn();
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        //Rebote contra otros objetos (Player y otros meteoritos)
        if (collision.gameObject.CompareTag("Meteorito") || collision.gameObject.CompareTag("Player"))
        {
            Vector2 reboteDireccion = (transform.position - collision.transform.position).normalized; // Invertir la dirección despues de rebote con objeto

            float velocidad;

            switch (tamano)
            {
                case MeteoritoTamano.L:
                    velocidad = configuracion.velocidad_Meteorito_L;
                    break;
                case MeteoritoTamano.M:
                    velocidad = configuracion.velocidad_Meteorito_M;
                    break;
                case MeteoritoTamano.S:
                    velocidad = configuracion.velocidad_Meteorito_S;
                    break;
                default:
                    velocidad = 0; //Aqui no tendria que entrar, se lo pongo para que no joda
                    break;
                   
                }

            rb.linearVelocity = reboteDireccion * velocidad;

        }
    }

}
