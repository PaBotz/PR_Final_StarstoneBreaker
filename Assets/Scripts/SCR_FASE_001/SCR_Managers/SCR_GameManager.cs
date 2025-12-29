using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// GameManager con soporte de red - FASE 2 (CORREGIDO V2)
// 
// CORRECCIONES:
// - Agregado IsSpawned check en Update() para evitar RPCs antes de que la red esté lista
// - Agregado IsSpawned check en todos los métodos que usan RPCs
public class SCR_GameManager : NetworkBehaviour
{
    public static SCR_GameManager Instancia { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GameObject prefab_Meteorito_L;
    [SerializeField] private Transform[] PuntosDeSpawn;

    private SCR_ConfiguracionJuego configuracion;

    // Puntajes por jugador
    private Dictionary<ulong, int> puntajesPorJugador = new Dictionary<ulong, int>();

    // NetworkVariables para sincronizar estado
    private NetworkVariable<float> tiempoRestante = new NetworkVariable<float>();
    private NetworkVariable<bool> juegoActivo = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> partidaIniciada = new NetworkVariable<bool>(false);

    private float siguienteMeteorito_Spawn;

    // Getters públicos
    public Dictionary<ulong, int> PuntajesPorJugador => puntajesPorJugador;
    public float TiempoRestante => tiempoRestante.Value;
    public bool JuegoActivo => juegoActivo.Value;
    public bool PartidaIniciada => partidaIniciada.Value;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Obtener configuración en Awake para asegurar que esté disponible
        configuracion = SCR_ConfiguracionJuego.Instancia;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Intentar obtener configuración si no se obtuvo en Awake
        if (configuracion == null)
        {
            configuracion = SCR_ConfiguracionJuego.Instancia;
        }

        if (configuracion == null)
        {
            Debug.LogError("SCR_ConfiguracionJuego.Instancia es NULL! Asegúrate de que exista en la escena.");
            return;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (!puntajesPorJugador.ContainsKey(NetworkManager.Singleton.LocalClientId))
            {
                puntajesPorJugador[NetworkManager.Singleton.LocalClientId] = 0;
            }

            Debug.Log("GameManager listo en servidor. Esperando que Host inicie la partida...");
        }

        partidaIniciada.OnValueChanged += OnPartidaIniciadaCambio;

        Debug.Log($"GameManager OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsSpawned: {IsSpawned}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        partidaIniciada.OnValueChanged -= OnPartidaIniciadaCambio;
    }

    void OnPartidaIniciadaCambio(bool anterior, bool nuevo)
    {
        if (nuevo)
        {
            Debug.Log("¡La partida ha comenzado!");
        }
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (!puntajesPorJugador.ContainsKey(clientId))
        {
            puntajesPorJugador[clientId] = 0;
            Debug.Log($"Jugador {clientId} conectado. Puntaje inicial: 0");
        }

        // IMPORTANTE: Solo enviar RPCs si estamos spawneados
        if (IsSpawned)
        {
            EnviarPuntajesATodos();
            NotificarJugadoresConectadosRpc(NetworkManager.Singleton.ConnectedClientsIds.Count);
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"Jugador {clientId} desconectado");

        if (IsSpawned)
        {
            NotificarJugadoresConectadosRpc(NetworkManager.Singleton.ConnectedClientsIds.Count);
        }
    }

    [Rpc(SendTo.Everyone)]
    void NotificarJugadoresConectadosRpc(int cantidad)
    {
        Debug.Log($"Jugadores conectados: {cantidad}");
    }

    // ========================================
    // MÉTODO PARA EL BOTÓN DE INICIAR
    // ========================================

    public void IniciarPartida()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Solo el Host puede iniciar la partida");
            return;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning("GameManager aún no está spawneado en la red");
            return;
        }

        if (partidaIniciada.Value)
        {
            Debug.LogWarning("La partida ya fue iniciada");
            return;
        }

        Debug.Log("Host iniciando partida...");
        partidaIniciada.Value = true;
        EmpezarJuego();

        JuegoEmpezadoRpc();
    }

    [Rpc(SendTo.Everyone)]
    void JuegoEmpezadoRpc()
    {
        Debug.Log("¡Juego iniciado para todos!");
        SCR_ConexionNetworkUI conexionUI = FindFirstObjectByType<SCR_ConexionNetworkUI>();
        if (conexionUI != null)
        {
            conexionUI.OcultarPanelEspera();
        }
    }

    void Start()
    {
        // Backup para obtener configuración
        if (configuracion == null)
        {
            configuracion = SCR_ConfiguracionJuego.Instancia;
        }
    }

    void Update()
    {
        // CRÍTICO: Verificar IsSpawned antes de cualquier operación de red
        if (!IsServer) return;
        if (!IsSpawned) return;  // <-- ESTA LÍNEA EVITA EL ERROR
        if (!juegoActivo.Value) return;

        ActualizarTimer();
        Controlador_MeteoritosSpawn();
    }

    void EmpezarJuego()
    {
        if (!IsServer) return;

        foreach (var clienteId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            puntajesPorJugador[clienteId] = 0;
        }

        tiempoRestante.Value = configuracion.duracionDeMatch;
        juegoActivo.Value = true;
        siguienteMeteorito_Spawn = Time.time + configuracion.intervalo_MeteoritoSpawn;

        // Solo enviar RPCs si estamos spawneados
        if (IsSpawned)
        {
            EnviarPuntajesATodos();
            ActualizarTimerRpc(tiempoRestante.Value);
        }

        Debug.Log($"Juego empezado! Duración: {configuracion.duracionDeMatch} segundos");
    }

    void ActualizarTimer()
    {
        tiempoRestante.Value -= Time.deltaTime;

        // El RPC ya está protegido por el IsSpawned en Update()
        ActualizarTimerRpc(tiempoRestante.Value);

        if (tiempoRestante.Value <= 0)
        {
            FinalizarJuego();
        }
    }

    void Controlador_MeteoritosSpawn()
    {
        if (Time.time >= siguienteMeteorito_Spawn)
        {
            MeteoritoSpawn();
            siguienteMeteorito_Spawn = Time.time + configuracion.intervalo_MeteoritoSpawn;
        }
    }

    void MeteoritoSpawn()
    {
        if (!IsServer) return;

        if (PuntosDeSpawn == null || PuntosDeSpawn.Length == 0)
        {
            Debug.LogError("No hay puntos de spawn asignados!");
            return;
        }

        Transform puntoDeSpawn = PuntosDeSpawn[Random.Range(0, PuntosDeSpawn.Length)];

        GameObject meteorito = Instantiate(prefab_Meteorito_L, puntoDeSpawn.position, Quaternion.identity);

        NetworkObject meteoritoNetObj = meteorito.GetComponent<NetworkObject>();
        if (meteoritoNetObj != null)
        {
            meteoritoNetObj.Spawn();
            Debug.Log($"Meteorito spawneado en {puntoDeSpawn.position}");
        }
        else
        {
            Debug.LogWarning("Meteorito no tiene NetworkObject!");
            Destroy(meteorito);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SumarPuntosServerRpc(ulong clienteID, int puntos)
    {
        if (!puntajesPorJugador.ContainsKey(clienteID))
        {
            puntajesPorJugador[clienteID] = 0;
        }

        puntajesPorJugador[clienteID] += puntos;
        puntajesPorJugador[clienteID] = Mathf.Max(0, puntajesPorJugador[clienteID]);

        Debug.Log($"Jugador {clienteID}: {puntajesPorJugador[clienteID]} puntos");

        if (IsSpawned)
        {
            EnviarPuntajesATodos();
        }
    }

    // Método auxiliar para enviar puntajes
    void EnviarPuntajesATodos()
    {
        if (!IsServer || !IsSpawned) return;

        ulong[] ids = puntajesPorJugador.Keys.ToArray();
        int[] puntos = puntajesPorJugador.Values.ToArray();
        EnviarPuntajesRpc(ids, puntos);
    }

    [Rpc(SendTo.Everyone)]
    void EnviarPuntajesRpc(ulong[] clienteIds, int[] puntajes)
    {
        Dictionary<ulong, int> puntajesRecibidos = new Dictionary<ulong, int>();
        for (int i = 0; i < clienteIds.Length; i++)
        {
            puntajesRecibidos[clienteIds[i]] = puntajes[i];
        }

        if (SCR_UIManager.Instancia != null)
        {
            SCR_UIManager.Instancia.ActualizarPuntajesMultijugador(puntajesRecibidos);
        }
    }

    [Rpc(SendTo.Everyone)]
    void ActualizarTimerRpc(float tiempo)
    {
        if (SCR_UIManager.Instancia != null)
        {
            SCR_UIManager.Instancia.ActualizarTimer(tiempo);
        }
    }

    void FinalizarJuego()
    {
        if (!IsServer) return;

        juegoActivo.Value = false;

        var ranking = puntajesPorJugador.OrderByDescending(x => x.Value).ToList();

        ulong[] clienteIds = new ulong[ranking.Count];
        int[] puntajes = new int[ranking.Count];

        for (int i = 0; i < ranking.Count; i++)
        {
            clienteIds[i] = ranking[i].Key;
            puntajes[i] = ranking[i].Value;
        }

        if (IsSpawned)
        {
            MostrarRankingRpc(clienteIds, puntajes);
        }

        Debug.Log("=== JUEGO TERMINADO ===");
        foreach (var jugador in ranking)
        {
            Debug.Log($"Jugador {jugador.Key}: {jugador.Value} puntos");
        }
    }

    [Rpc(SendTo.Everyone)]
    void MostrarRankingRpc(ulong[] clienteIds, int[] puntajes)
    {
        List<KeyValuePair<ulong, int>> rankingList = new List<KeyValuePair<ulong, int>>();

        for (int i = 0; i < clienteIds.Length; i++)
        {
            rankingList.Add(new KeyValuePair<ulong, int>(clienteIds[i], puntajes[i]));
        }

        if (SCR_UIManager.Instancia != null)
        {
            SCR_UIManager.Instancia.MostrarRanking(rankingList);
        }
    }

    public void RestarJuego()
    {
        if (!IsServer) return;

        foreach (GameObject meteorito in GameObject.FindGameObjectsWithTag("Meteorito"))
        {
            NetworkObject netObj = meteorito.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            Destroy(meteorito);
        }

        foreach (GameObject powerUp in GameObject.FindGameObjectsWithTag("PowerUp"))
        {
            NetworkObject netObj = powerUp.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            Destroy(powerUp);
        }

        // Reiniciar estado
        partidaIniciada.Value = false;
        EmpezarJuego();
        partidaIniciada.Value = true;
    }

    public int ObtenerPuntaje(ulong clientId)
    {
        if (puntajesPorJugador.ContainsKey(clientId))
        {
            return puntajesPorJugador[clientId];
        }
        return 0;
    }

    public ulong ObtenerGanador()
    {
        if (puntajesPorJugador.Count == 0) return 0;
        return puntajesPorJugador.OrderByDescending(x => x.Value).First().Key;
    }
}