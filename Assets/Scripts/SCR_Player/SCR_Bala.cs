using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class SCR_Bala : MonoBehaviour
{
    private Rigidbody2D rb;
    private SCR_ConfiguracionJuego configuracion; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        configuracion = SCR_ConfiguracionJuego.Instancia;

        rb.linearVelocity = Vector2.up * configuracion.velocidad_Bala;

        Destroy(gameObject, configuracion.bala_Lifetime); //Asi evitamos llamadas inecesarias en update. 
    }


    void Update()
    {
        //Destruir si se sale del escenario
        if(transform.position.y > configuracion.maxY) 
        {
            Destroy(gameObject);
        }
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Meteorito"))
        {
            SCR_Meteorito meteorito = other.GetComponent< SCR_Meteorito>();
            meteorito.RecibirDano();
            Destroy(gameObject);
            Debug.Log("Meteorito hizo contacto");
        }


    }

}

