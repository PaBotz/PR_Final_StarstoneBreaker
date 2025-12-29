using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

// FASE 2 - CORREGIDO PARA NETWORKTRANSFORM
// El cliente envía input al servidor, el servidor mueve el player
// Así funciona con NetworkTransform normal (sin ClientNetworkTransform)

[RequireComponent(typeof(Rigidbody2D))]
public class SCR_PlayerController : NetworkBehaviour
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

    private NetworkVariable<float> velocidad_JugadorActual = new NetworkVariable<float>();
    private NetworkVariable<float> cadenciaDeDisparoActual = new NetworkVariable<float>();
    private NetworkVariable<bool> estaAturdido = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> tieneEscudo = new NetworkVariable<bool>(false);

    public bool TieneEscudo => tieneEscudo.Value;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;

        if (configuracion == null)
        {
            Debug.LogError("SCR_ConfiguracionJuego.Instancia es NULL!");
            return;
        }

        if (IsServer)
        {
            velocidad_JugadorActual.Value = configuracion.velocidad_Jugador;
            cadenciaDeDisparoActual.Value = configuracion.cadencia_Disparo;
        }

        Debug.Log($"Player Start - IsOwner: {IsOwner}, IsServer: {IsServer}, OwnerClientId: {OwnerClientId}");
    }

    void Update()
    {
        // Solo el owner puede enviar input
        if (!IsOwner) return;

        // Disparo
        if (!estaAturdido.Value && Input.GetKey(teclaDisparo))
        {
            ControladorDisparo();
        }
    }

    private void FixedUpdate()
    {
        // CAMBIO CLAVE: El owner lee el input y lo envía al servidor
        if (IsOwner && !estaAturdido.Value)
        {
            // Enviar movimiento al servidor
            if (movimientoLateral != 0)
            {
                MoverServerRpc(movimientoLateral);
            }
        }
    }

    // NUEVO: El servidor recibe el input y mueve el player
    [ServerRpc]
    void MoverServerRpc(float direccion)
    {
        if (estaAturdido.Value) return;
        if (configuracion == null) return;

        float targetX = rb.position.x + direccion * velocidad_JugadorActual.Value * Time.fixedDeltaTime;
        targetX = Mathf.Clamp(targetX, configuracion.minX, configuracion.maxX);

        rb.MovePosition(new Vector2(targetX, rb.position.y));
    }

    // Input System callback - Solo el owner lo usa
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        movimientoLateral = value.Get<Vector2>().x;
    }

    void ControladorDisparo()
    {
        if (Time.time >= siguienteDisparo)
        {
            DisparoServerRpc(puntoDeDisparo.position);
            siguienteDisparo = Time.time + cadenciaDeDisparoActual.Value;
        }
    }

    [ServerRpc]
    void DisparoServerRpc(Vector2 posicion)
    {
        if (balaPrefab == null)
        {
            Debug.LogWarning("balaPrefab no asignado!");
            return;
        }

        GameObject bala = Instantiate(balaPrefab, posicion, Quaternion.identity);

        NetworkObject balaNetworkObject = bala.GetComponent<NetworkObject>();
        if (balaNetworkObject == null)
        {
            Debug.LogWarning("La bala no tiene NetworkObject!");
            Destroy(bala);
            return;
        }

        balaNetworkObject.Spawn();

        SCR_Bala balaScript = bala.GetComponent<SCR_Bala>();
        if (balaScript != null)
        {
            balaScript.AsignarDueno(OwnerClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AplicarAturdimientoRpc()
    {
        if (tieneEscudo.Value)
        {
            tieneEscudo.Value = false;
        }
        else
        {
            estaAturdido.Value = true;
            Invoke(nameof(RemoverAturdimiento), configuracion.duracion_Aturdimiento);
        }
    }

    void RemoverAturdimiento()
    {
        if (IsServer)
        {
            estaAturdido.Value = false;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AplicarBoostDeDisparoRpc()
    {
        cadenciaDeDisparoActual.Value = configuracion.cadencia_Disparo / configuracion.disparoBoostMultiplicador;
        Invoke(nameof(RestarCadenciaDeDisparo), configuracion.duracion_PowerUp);
    }

    void RestarCadenciaDeDisparo()
    {
        if (IsServer)
        {
            cadenciaDeDisparoActual.Value = configuracion.cadencia_Disparo;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AplicarBoost_VelocidadRpc()
    {
        velocidad_JugadorActual.Value = configuracion.velocidad_Jugador * configuracion.velocidadBoost_Multiplicador;
        Invoke(nameof(RestarVelocidad), configuracion.duracion_PowerUp);
    }

    void RestarVelocidad()
    {
        if (IsServer)
        {
            velocidad_JugadorActual.Value = configuracion.velocidad_Jugador;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AplicarEscudoSeverRpc()
    {
        tieneEscudo.Value = true;
        Debug.Log("TieneEscudo");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ManejarColisionMeteoroRpc()
    {
        if (tieneEscudo.Value)
        {
            tieneEscudo.Value = false;
            MostrarEscudoRotoRpc();
        }
        else
        {
            estaAturdido.Value = true;
            Invoke(nameof(RemoverAturdimiento), configuracion.duracion_Aturdimiento);
            SCR_GameManager.Instancia?.SumarPuntosServerRpc(OwnerClientId, configuracion.golpe_Penalizacion);
            MostrarPenalizacionRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    void MostrarEscudoRotoRpc()
    {
        if (SCR_TextoFlotanteManager.Instancia != null)
        {
            SCR_TextoFlotanteManager.Instancia.MostrarBonificacion("¡Escudo Roto!", transform.position);
        }
    }

    [Rpc(SendTo.Everyone)]
    void MostrarPenalizacionRpc()
    {
        if (SCR_TextoFlotanteManager.Instancia != null)
        {
            SCR_TextoFlotanteManager.Instancia.MostrarPuntaje(configuracion.golpe_Penalizacion, transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.gameObject.CompareTag("Meteorito"))
        {
            ManejarColisionMeteoroRpc();
        }
    }
}