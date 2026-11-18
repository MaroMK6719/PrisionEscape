using UnityEngine;

namespace Platybytes.Llave
{
    // Pared que se desliza con la inclinación del móvil, igual que la bola, dentro de un recorrido limitado.
    public class ParedMovil : MonoBehaviour
    {
        Vector2 origen;
        Vector2 eje;
        float recorrido;
        const float Velocidad = 3f;
        Rigidbody2D rb;

        public void Configurar(Vector2 eje, float recorrido)
        {
            this.eje = eje;
            this.recorrido = recorrido;
            origen = transform.position;
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            float cantidad = Vector2.Dot(RescataLaLlave.Inclinacion, eje);
            Vector2 objetivo = origen + eje * (cantidad * recorrido);
            rb.MovePosition(Vector2.MoveTowards(rb.position, objetivo, Velocidad * Time.fixedDeltaTime));
        }
    }
}
