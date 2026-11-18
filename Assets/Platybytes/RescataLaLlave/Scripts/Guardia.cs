using UnityEngine;

namespace Platybytes.Llave
{
    // Guardia del laberinto. Si tiene recorrido, patrulla de lado a lado; si no, se queda quieto.
    public class Guardia : MonoBehaviour
    {
        Vector2 origen;
        float recorrido;   // celdas a cada lado
        float velocidad;
        Rigidbody2D rb;

        public void Configurar(float recorrido, float velocidad)
        {
            this.recorrido = recorrido;
            this.velocidad = velocidad;
            origen = transform.position;
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            if (recorrido <= 0f || rb == null) return;
            float desplazamiento = Mathf.PingPong(Time.time * velocidad, recorrido * 2f) - recorrido;
            rb.MovePosition(origen + Vector2.right * desplazamiento);
        }
    }
}
