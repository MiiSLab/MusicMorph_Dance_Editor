using UnityEngine;
using System;

namespace Clicknext.Customization
{
    public class CharacterColor : MonoBehaviour
    {
        [Serializable]
        private struct RendererTarget
        {
            public int materialIndex;
            public Renderer[] renderers;
        }

        [SerializeField] RendererTarget hair;
        [SerializeField] RendererTarget eye;
        [SerializeField] RendererTarget skin;
        [SerializeField] RendererTarget face;

        public void SetHairColor(Color[] colors)
        {
            foreach (var each in hair.renderers)
            {
                var mats = SetMaterials(each, colors, hair.materialIndex);
                each.GetComponent<SkinnedMeshRenderer>().materials = mats;
            }
        }

        public void SetEyeColor(Color[] colors)
        {
            foreach (var each in eye.renderers)
            {
                var mats = SetMaterials(each, colors, eye.materialIndex);
                each.GetComponent<MeshRenderer>().materials = mats;
            }
        }

        public void SetSkinColor(Color[] colors)
        {
            foreach (var each in skin.renderers)
            {
                var mats = SetMaterials(each, colors, skin.materialIndex);
                each.GetComponent<SkinnedMeshRenderer>().materials = mats;
            }

            foreach (var each in face.renderers)
            {
                var mats = SetMaterials(each, colors, face.materialIndex);
                each.GetComponent<SkinnedMeshRenderer>().materials = mats;
            }
        }

        public Material[] SetMaterials(Renderer renderers, Color[] colors, int index)
        {
            var material = renderers.materials[index];
            material.SetColor("_BaseColor", colors[0]);
            material.SetColor("_1st_ShadeColor", colors[1]);
            material.SetColor("_2nd_ShadeColor", colors[2]);
            material.SetColor("_HighColor", colors[3]);
            material.SetColor("_Outline_Color", colors[2]);
            renderers.materials[index] = material;
            return renderers.materials;
        }
    }
}