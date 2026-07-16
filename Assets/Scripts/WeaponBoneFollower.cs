using UnityEngine;

// Faz a arma SEGUIR um osso do esqueleto sem ser filha dele: copia posição e
// rotação do osso a cada LateUpdate (depois do Animator), mas NUNCA a escala.
// Motivo: alguns clips do Mixamo animam a ESCALA dos ossos (a de morte, por
// exemplo) — uma arma filha do osso herdava isso e ficava gigante.
public class WeaponBoneFollower : MonoBehaviour
{
    public Transform bone;
    public Vector3 positionOffset;  // no espaço do osso (usa a rotação, NÃO a escala)
    public Vector3 eulerOffset;

    void LateUpdate()
    {
        if (bone == null) return;
        transform.position = bone.position + bone.rotation * positionOffset;
        transform.rotation = bone.rotation * Quaternion.Euler(eulerOffset);
    }
}
