using System.Collections;
using UnityEngine;

namespace Platybytes.Llave
{
    // Bola que lleva la llave. Se mueve con la inclinación y avisa al minijuego de lo que toca.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bola : MonoBehaviour
    {
        const float RozamientoNormal = 0.8f;
        const float RozamientoPegajoso = 14f;   // al tocar una pared pegajosa la bola casi se para
        const float VelocidadMaxima = 8f;
        const float EmpujeRebote = 7f;
        const float EmpujeGolpe = 6f;
        const float TiempoInvulnerable = 1f;

        RescataLaLlave juego;
        Rigidbody2D rb;
        SpriteRenderer sr;
        bool activa;
        bool invulnerable;
        int paredesPegajosasTocando;

        public void Configurar(RescataLaLlave juego)
        {
            this.juego = juego;
            sr = GetComponent<SpriteRenderer>();

            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = RozamientoNormal;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.simulated = false; // quieta hasta que desaparece la instrucción

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.48f; // en espacio local (la bola está escalada a 0.72)

            PhysicsMaterial2D material = new PhysicsMaterial2D("Bola");
            material.friction = 0f;
            material.bounciness = 0.15f;
            col.sharedMaterial = material;
        }

        public void Activar()
        {
            activa = true;
            rb.simulated = true;
        }

        public void Detener()
        {
            activa = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        public void Recolocar(Vector2 posicion)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = posicion;
            transform.position = posicion;
            paredesPegajosasTocando = 0;
            rb.linearDamping = RozamientoNormal;
        }

        void FixedUpdate()
        {
            if (!activa) return;

            rb.AddForce(RescataLaLlave.Inclinacion * juego.FuerzaMaxima);

            if (rb.linearVelocity.magnitude > VelocidadMaxima)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * VelocidadMaxima;
            }
        }

        // ---------------- Paredes especiales ----------------

        void OnCollisionEnter2D(Collision2D choque)
        {
            if (choque.collider.GetComponent<ParedRebote>() != null)
            {
                // Impulso en la dirección contraria a la pared
                Vector2 normal = choque.GetContact(0).normal;
                rb.AddForce(normal * EmpujeRebote, ForceMode2D.Impulse);
                Sonidos.Rebote();
            }

            if (choque.collider.GetComponent<ParedPegajosa>() != null)
            {
                paredesPegajosasTocando++;
                rb.linearDamping = RozamientoPegajoso;
            }
        }

        void OnCollisionExit2D(Collision2D choque)
        {
            if (choque.collider.GetComponent<ParedPegajosa>() != null)
            {
                paredesPegajosasTocando = Mathf.Max(0, paredesPegajosasTocando - 1);
                if (paredesPegajosasTocando == 0) rb.linearDamping = RozamientoNormal;
            }
        }

        // ---------------- Guardias, estrella y cerradura ----------------

        void OnTriggerEnter2D(Collider2D otro)
        {
            if (!activa) return;

            EstrellaNivel estrella = otro.GetComponent<EstrellaNivel>();
            if (estrella != null)
            {
                juego.EstrellaRecogida(estrella);
                return;
            }

            if (otro.GetComponent<CerraduraNivel>() != null)
            {
                juego.CerraduraAlcanzada();
                return;
            }

            ComprobarGuardia(otro);
        }

        // Si la bola sigue dentro de un guardia cuando se acaba la invulnerabilidad, vuelve a contar
        void OnTriggerStay2D(Collider2D otro)
        {
            if (!activa) return;
            ComprobarGuardia(otro);
        }

        void ComprobarGuardia(Collider2D otro)
        {
            if (invulnerable) return;
            if (otro.GetComponent<Guardia>() == null) return;

            // Sale despedida hacia atrás, lejos del guardia
            Vector2 direccion = ((Vector2)transform.position - (Vector2)otro.transform.position).normalized;
            if (direccion == Vector2.zero) direccion = -rb.linearVelocity.normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direccion * EmpujeGolpe, ForceMode2D.Impulse);

            StartCoroutine(Invulnerabilidad());
            juego.GolpeDeGuardia();
        }

        IEnumerator Invulnerabilidad()
        {
            invulnerable = true;
            float t = 0f;
            while (t < TiempoInvulnerable)
            {
                t += Time.deltaTime;
                // Parpadeo suave (no destellos de pantalla completa)
                sr.color = new Color(1f, 1f, 1f, 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 12f)));
                yield return null;
            }
            sr.color = Color.white;
            invulnerable = false;
        }
    }
}
