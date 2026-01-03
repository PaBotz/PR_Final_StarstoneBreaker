using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

// Maneja la UI de conexión, inicio de Host/Client, y el botón de Iniciar Partida
public class SCR_ConexionNetworkUI : MonoBehaviour
{
    [Header("UI Referencias - Conexión")]
    [SerializeField] private Button boton_Host;
    [SerializeField] private Button boton_Cliente;
    [SerializeField] private TextMeshProUGUI texto_Estado;
    [SerializeField] private GameObject panel_Conexion;
    [SerializeField] private GameObject panel_Ranking;
    [SerializeField] private GameObject panel_FinJuego;
    [SerializeField] private GameObject panel_Puntajes;

    [Header("UI Referencias - Sala de Espera (NUEVO)")]
    [SerializeField] private GameObject panel_Espera;              // Panel que se muestra mientras esperan
    [SerializeField] private TextMeshProUGUI texto_JugadoresConectados; // "Jugadores: 2"
    [SerializeField] private Button boton_IniciarPartida;          // Solo visible para el Host

    [Header("Network Referencias")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport transport;

    [Header("Relay Fase3")]
    public TextMeshProUGUI codigoRelay;
    public TMP_InputField inputField_IP;




    void Awake()
    {
        // Configurar botones aquí - Awake es más confiable
        if (boton_Host != null)
        {
            boton_Host.onClick.RemoveAllListeners(); 
            boton_Host.onClick.AddListener(IniciarHost);
        }

        if (boton_Cliente != null)
        {
            boton_Cliente.onClick.RemoveAllListeners();
            boton_Cliente.onClick.AddListener(IniciarCliente);
        }
    }
    async public void Start()
    {
        //1. iniciar los servicios de la nube

        await UnityServices.InitializeAsync();

        //2. Autenticar al usuario (anonimo)

        await AuthenticationService.Instance.SignInAnonymouslyAsync();



        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }

        if (transport == null)
        {
            transport = FindFirstObjectByType<UnityTransport>();
        }

        /*
        // Configurar botones de conexión
        if (boton_Host != null)
        {
            boton_Host.onClick.AddListener(IniciarHost);
        }
                                                                     //Comentado, porque está generando conflictos en los botones.
        if (boton_Cliente != null)
        {
            boton_Cliente.onClick.AddListener(IniciarCliente);
        } 
        */
        // Configurar botón de iniciar partida

        if (boton_IniciarPartida != null)
        {
            boton_IniciarPartida.onClick.AddListener(IniciarPartida);
            boton_IniciarPartida.gameObject.SetActive(false); // Oculto hasta que sea Host
        }

        // IP por defecto
       /* if (inputField_IP != null && string.IsNullOrEmpty(inputField_IP.text))
        {
            inputField_IP.text = "127.0.0.1";
        } */ // Fase 2

        // Ocultar panel de espera al inicio
        if (panel_Espera != null)
        {
            panel_Espera.SetActive(false);
        }
    }

    // Inicia el juego como Host (servidor + cliente)
    public async void IniciarHost()
    {
        //3. Pedirle a Unity el espacio en la nube (allocation)   //Crear el espacio en la nube para iniciar el juego

        Allocation serverUnity = await RelayService.Instance.CreateAllocationAsync(4);



        //6. Configuracion del Unity Transport

            //6.1 Para que se conecte con el relay asignado

        RelayServerData datosRelay = AllocationUtils.ToRelayServerData(serverUnity, "udp");

            //6.2 buscar el componente UnityTransport

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(datosRelay);

        //7 Conectar (crea el server)


        ActualizarTextoEstado("Inicializando como Host...");

        
        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
        //4.Obtener el codigo de la partida    //aqui te da el Id del espacio creado arriba

        string codigoPartida = await RelayService.Instance.GetJoinCodeAsync(serverUnity.AllocationId);

        //5. Le damos el valor del Id del espacio creado al TextMeshProUGUI con el codigo
        codigoRelay.text = codigoPartida;
            ActualizarTextoEstado("Host Inicializado! Esperando Clientes...");
            EsconderConexionUI();
            MostrarPanelEspera(true); // NUEVO: Mostrar panel de espera

            // Suscribirse a eventos de conexión
            networkManager.OnClientConnectedCallback += ConectarCliente;
        }
        else
        {
            ActualizarTextoEstado("ERROR: Fallo al conectar Host!", true);
        }
    }

    // Inicia el juego como Client (solo cliente)
    public async void IniciarCliente()
    {
        // 1. Obtener el codigo del relay a partir del codigo de la partida

        string codigoPartida = inputField_IP.text;

        JoinAllocation serverDeUnity = await RelayService.Instance.JoinAllocationAsync(codigoPartida);

        //2.

        RelayServerData datosRelay = AllocationUtils.ToRelayServerData(serverDeUnity, "udp");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(datosRelay);

      
        ActualizarTextoEstado("Inicializando como Cliente...");

        if (inputField_IP == null || string.IsNullOrEmpty(inputField_IP.text))
        {
            ActualizarTextoEstado("ERROR: Por favor, ingrese una IP address!", true);
            return;
        }

        // Configurar la IP antes de conectar
        if (transport != null)
        {
            transport.ConnectionData.Address = inputField_IP.text;
        }

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            ActualizarTextoEstado("Conectando...");

            // Suscribirse a eventos de conexión
            networkManager.OnClientConnectedCallback += ConectarCliente;
            networkManager.OnClientDisconnectCallback += DesconectarCliente;
        }
        else
        {
            ActualizarTextoEstado("ERROR: Fallo al conectar Cliente!", true);
        }
    }

    // Callback cuando un cliente se conecta
    private void ConectarCliente(ulong clienteID)
    {

        if (NetworkManager.Singleton.IsHost)
        {
            ActualizarTextoEstado($"Cliente {clienteID} conectado!");
            ActualizarContadorJugadores();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            ActualizarTextoEstado("Conectado al servidor! Esperando que Host inicie...");
            EsconderConexionUI();
            MostrarPanelEspera(false); // Cliente ve panel pero sin botón de iniciar
        }
    }

    // Callback cuando un cliente se desconecta
    private void DesconectarCliente(ulong clienteID)
    {
        ActualizarTextoEstado("Desconectado del servidor", true);
        OcultarPanelEspera();
        MuestraConexionUI();
    }

    // ========================================
    // NUEVO: LÓGICA DEL PANEL DE ESPERA
    // ========================================

    void MostrarPanelEspera(bool esHost)
    {
        if (panel_Espera != null)
        {
            panel_Espera.SetActive(true);
        }

        // Solo el Host puede ver el botón de iniciar
        if (boton_IniciarPartida != null)
        {
            boton_IniciarPartida.gameObject.SetActive(esHost);
        }

        ActualizarContadorJugadores();
    }

    // NUEVO: Llamado por SCR_GameManager cuando el juego empieza
    public void OcultarPanelEspera()
    {
        if (panel_Espera != null)
        {
            panel_Espera.SetActive(false);
        }
    }

    void ActualizarContadorJugadores()
    {
        if (texto_JugadoresConectados != null && NetworkManager.Singleton != null)
        {
            int cantidad = NetworkManager.Singleton.ConnectedClientsIds.Count;
            texto_JugadoresConectados.text = $"Jugadores conectados: {cantidad}";
        }
    }

    // NUEVO: Botón para que el Host inicie la partida
    void IniciarPartida()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Solo el Host puede iniciar la partida");
            return;
        }

        // Llamar al GameManager para que inicie el juego
        if (SCR_GameManager.Instancia != null)
        {
            SCR_GameManager.Instancia.IniciarPartida();
        }
        else
        {
            Debug.LogError("SCR_GameManager.Instancia es NULL! Asegúrate de que exista en la escena.");
        }
    }

    // ========================================
    // MÉTODOS AUXILIARES
    // ========================================

    private void ActualizarTextoEstado(string mensaje, bool daError = false)
    {
        if (texto_Estado == null)
        {
            Debug.LogWarning("texto_Estado no referenciado");
            return;
        }

        texto_Estado.text = mensaje;
        texto_Estado.color = daError ? Color.red : Color.green;

        Debug.Log($"[NetworkConnection] {mensaje}");
    }

    private void EsconderConexionUI()
    {
        if (panel_Conexion != null)
        {
            panel_Conexion.SetActive(false);
        }
    }

    private void MuestraConexionUI()
    {
        if (panel_Conexion != null)
        {
            panel_Conexion.SetActive(true);
        }
    }

    // Limpiar suscripciones cuando se destruya
    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= ConectarCliente;
            networkManager.OnClientDisconnectCallback -= DesconectarCliente;
        }
    }

    public void Desconectar()
    {
        Debug.Log("Desconectando...");

        // Si soy cliente, avisar al servidor antes de desconectar
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            // Avisar al GameManager que me voy
            if (SCR_GameManager.Instancia != null)
            {
                SCR_GameManager.Instancia.ClienteSeDesconectaServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }


        NetworkManager.Singleton.Shutdown();
        panel_Conexion.SetActive(true);
        panel_FinJuego.SetActive(false);
        panel_Puntajes.SetActive(false);
        panel_Ranking.SetActive(false);
        panel_Espera.SetActive(false);
        
    }
    public void SalirAplicacion() 
    {
        Debug.Log("Saliendo");
        Application.Quit();
    }
}