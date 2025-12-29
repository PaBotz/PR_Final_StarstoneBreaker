using UnityEngine;
using Unity.Netcode;

// Bala con soporte de red - FASE 2 CORREGIDO
// - Solo el servidor puede destruir la bala
// - Update ya no tiene Destroy (eso causaba el error)

[RequireComponent(typeof(Rigidbody2D))]
public class SCR_Bala : NetworkBehaviour
{
    private Rigidbody2D rb;
    private SCR_ConfiguracionJuego configuracion;
    private NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;
        rb.linearVelocity = Vector2.up * configuracion.velocidad_Bala;

        // Solo el servidor programa la destrucción
        if (IsServer)
        {
            Invoke(nameof(DestruirBala), configuracion.bala_Lifetime);
        }
    }

    void Update()
    {
        // CORREGIDO: Solo el servidor puede destruir
        if (!IsServer) return;

        // Destruir si se sale del escenario
        if (transform.position.y > configuracion.maxY)
        {
            DestruirBala();
        }
    }

    public void AsignarDueno(ulong clienteId)
    {
        if (IsServer)
        {
            ownerClientId.Value = clienteId;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo el servidor procesa colisiones
        if (!IsServer) return;

        if (other.gameObject.CompareTag("Meteorito"))
        {
            SCR_Meteorito meteorito = other.GetComponent<SCR_Meteorito>();
            if (meteorito != null)
            {
                meteorito.RecibirDanoRpc(ownerClientId.Value);
            }
            DestruirBala();
        }
    }

    void DestruirBala()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        Destroy(gameObject);
    }
}