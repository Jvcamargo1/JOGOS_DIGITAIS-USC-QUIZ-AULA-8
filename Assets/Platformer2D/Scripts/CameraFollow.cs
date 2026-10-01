using UnityEngine;

/// <summary>
/// Câmera que segue o alvo (o jogador) suavemente.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Posição da câmera em relação ao alvo. O Z deve ficar negativo (ex.: -10).")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [Tooltip("Tempo aproximado (s) para a câmera alcançar o alvo. Menor = mais colada.")]
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    // LateUpdate: depois que o jogador já se moveu no frame, evita tremidas na câmera.
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
