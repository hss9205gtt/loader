using UnityEngine;

namespace NumberBlocks
{
    /// <summary>성공했을 때 터지는 알록달록 색종이 조각.</summary>
    public class Confetti : MonoBehaviour
    {
        Vector3 velocity;
        Vector3 spin;
        float life;

        public static void Burst(Vector3 at, int count = 40)
        {
            for (int i = 0; i < count; i++)
            {
                var color = NumberInfo.ColorOf(Random.Range(1, NumberInfo.MaxValue + 1));
                var go = Art.MeshObject("Confetti", null, Art.RoundedCube, color, at, new Vector3(0.18f, 0.18f, 0.05f));
                var c = go.AddComponent<Confetti>();
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.8f;
                c.velocity = dir * Random.Range(4f, 7f);
                c.spin = Random.insideUnitSphere * 720f;
                c.life = Random.Range(1.2f, 1.8f);
                go.transform.rotation = Random.rotation;
            }
        }

        void Update()
        {
            velocity += Vector3.down * 9f * Time.deltaTime;
            velocity *= 1f - 1.5f * Time.deltaTime; // 공기 저항
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime);
            life -= Time.deltaTime;
            if (life < 0.3f) transform.localScale *= 1f - 5f * Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
