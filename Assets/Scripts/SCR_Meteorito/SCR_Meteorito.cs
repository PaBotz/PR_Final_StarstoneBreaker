using System.Collections;
using System.Drawing;
using UnityEngine;
using static UnityEngine.Rendering.STP;
using static UnityEngine.RuleTile.TilingRuleOutput;

[RequireComponent(typeof(Rigidbody2D))]
public class SCR_Meteorito : MonoBehaviour
{
    public enum MeteoritoTamano { L, M, S }

    [SerializeField] private MeteoritoTamano tamano = MeteoritoTamano.L;
    [SerializeField] private GameObject prefab_Meteorito_M;
    [SerializeField] private GameObject prefab_Meteorito_S;
    [SerializeField] private GameObject[] bonificadores_Prefabs; // Array de prefabs de power-ups

    private Rigidbody2D rb;
    private SCR_ConfiguracionJuego configuracion;
    private Vector2 direccion;
    private int saludActual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;

        IniciarMovimiento();
        IniciarVida();

    }

    private void Update()
    {
        RevisarLimites();
    }

    void IniciarVida()
    {
        switch (tamano)
        {
            case MeteoritoTamano.L:
                saludActual = configuracion.vida_Meteorito_L;
                break;
            case MeteoritoTamano.M:
                saludActual = configuracion.vida_Meteorito_M;
                break;
            case MeteoritoTamano.S:
                saludActual = configuracion.vida_Meteorito_S;
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
        Vector2 posicion = transform.position;
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
            transform.position = posicion;
        }

    }

    public void RecibirDano()
    {
        saludActual--;
        if(saludActual <= 0)
        {
            //Probabilidad de que salga un power up
            LanzarDadoPowerUp();

            //Suma Puntos
            SCR_GameManager.Instancia.SumarPuntos(ObtenerPuntos_PorTamano());

            //Dividir el meteoro en meteoritos
            SpawnFragmentos();

            //Puntos flotantesManager
            SCR_TextoFlotanteManager.Instancia.MostrarPuntaje(ObtenerPuntos_PorTamano(), transform.position); 

            Destroy(gameObject);

        }

       
    }

    void LanzarDadoPowerUp()
    {
        if (bonificadores_Prefabs.Length != 0)
        {
            if (Random.value < configuracion.ProbabilidadDeBonificacion)
            {
                GameObject bonificacionAleatoria = bonificadores_Prefabs[Random.Range(0, bonificadores_Prefabs.Length)];
                Instantiate(bonificacionAleatoria, transform.position, Quaternion.identity);
            }
        }
    }

    int ObtenerPuntos_PorTamano() //NO SE LLAMA
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

   /* void SpawnFragmentos()
    {
        for (int i = 0; i < configuracion.fragmentosPorMeteorito; i++)
        {
            GameObject fragmento = Instantiate(prefab_Meteorito_S, transform.position, Quaternion.identity);

            // Dar direccion aleatoria a cada fragmento??? -> Creo que esto simplemente es una forma de asegurarse que el objeto instanciado tenga el SCR, correcto. Ni siquiera se si es necesrio, luego lo compruebo.
            /*SCR_Meteorito fragmentoMeteorito = fragmento.GetComponent<SCR_Meteorito>();
            fragmentoMeteorito.tamano = MeteoritoTamano.S; 
        }
    }            
*/

    void SpawnFragmentos()
    {
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

    //if (fragmentoPrefab == null) return;

    // Spawear fragmentos
    for (int i = 0; i < configuracion.fragmentosPorMeteorito; i++)
    {
        GameObject fragmento = Instantiate(fragmentoPrefab, transform.position, Quaternion.identity);

        // Configurar el tamaño del fragmento
        SCR_Meteorito fragmentosMeteorito = fragmento.GetComponent<SCR_Meteorito>();
        if (fragmentosMeteorito != null)
        {
                fragmentosMeteorito.tamano = nuevoTamano;
        }
    }
}

private void OnCollisionEnter2D(Collision2D collision)
    {
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
