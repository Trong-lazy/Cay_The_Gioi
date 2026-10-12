using System;
using CayTheGioi.Domain;
using UnityEngine;

namespace CayTheGioi.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CosmeticView : MonoBehaviour
    {
        private CosmeticSystem cosmeticSystem;
        private TreeView treeView;
        private HealthStatusSystem healthSystem;
        private EvolutionSystem evolutionSystem;
        private RebirthSystem rebirthSystem;

        private GameObject glowVisual;

        public bool IsPreviewing { get; private set; }
        public string ActiveVisualId { get; private set; } = "none";

        public void Bind(CosmeticSystem cosmetics, TreeView tree, HealthStatusSystem health = null, EvolutionSystem evolution = null, RebirthSystem rebirth = null)
        {
            Unsubscribe();

            cosmeticSystem = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            treeView = tree ?? throw new ArgumentNullException(nameof(tree));
            healthSystem = health;
            evolutionSystem = evolution;
            rebirthSystem = rebirth;

            if (cosmeticSystem != null)
            {
                cosmeticSystem.Equipped += OnEquippedChanged;
                cosmeticSystem.Unequipped += OnEquippedChanged;
            }
            if (healthSystem != null)
            {
                healthSystem.HealthChanged += OnHealthChanged;
                healthSystem.RunDied += CancelPreview;
            }
            if (evolutionSystem != null)
            {
                evolutionSystem.StageChanged += OnStageChanged;
            }
            if (rebirthSystem != null)
            {
                rebirthSystem.Completed += OnRebirthCompleted;
            }

            IsPreviewing = false;
            UpdateVisual(cosmeticSystem.EquippedCosmeticId);
        }

        public void SetPreview(string cosmeticId)
        {
            IsPreviewing = true;
            UpdateVisual(cosmeticId);
        }

        public void CancelPreview()
        {
            IsPreviewing = false;
            UpdateVisual(cosmeticSystem != null ? cosmeticSystem.EquippedCosmeticId : "none");
        }

        public void Refresh()
        {
            if (!IsPreviewing)
            {
                UpdateVisual(cosmeticSystem != null ? cosmeticSystem.EquippedCosmeticId : "none");
            }
        }

        private void OnEquippedChanged(string cosmeticId)
        {
            if (!IsPreviewing)
            {
                UpdateVisual(cosmeticSystem.EquippedCosmeticId);
            }
        }

        private void OnHealthChanged()
        {
            if (healthSystem != null && healthSystem.Hp <= 0 && IsPreviewing)
            {
                CancelPreview();
            }
        }

        private void OnStageChanged(string newStageId)
        {
            if (IsPreviewing)
            {
                CancelPreview();
            }
            else
            {
                UpdateVisual(ActiveVisualId);
            }
        }

        private void OnRebirthCompleted()
        {
            CancelPreview();
        }

        private void UpdateVisual(string cosmeticId)
        {
            ActiveVisualId = string.IsNullOrWhiteSpace(cosmeticId) ? "none" : cosmeticId;

            if (ActiveVisualId == "glow_placeholder")
            {
                ShowGlowPlaceholder(true);
            }
            else
            {
                ShowGlowPlaceholder(false);
            }
        }

        private void ShowGlowPlaceholder(bool show)
        {
            if (!show)
            {
                if (glowVisual != null) glowVisual.SetActive(false);
                return;
            }

            Transform anchor = treeView != null ? treeView.TrunkAnchor : transform;
            if (anchor == null) anchor = transform;

            if (glowVisual == null)
            {
                glowVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glowVisual.name = "GlowPlaceholder_Effect";

                var col = glowVisual.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = glowVisual.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Sprites/Default"));
                    // Màu xanh ngọc bích phát sáng (Cyan / Jade Glow), phân biệt hoàn toàn với màu đỏ cam của Burning
                    rend.material.color = new Color(0.2f, 0.95f, 0.78f, 0.45f);
                }
            }

            glowVisual.transform.SetParent(anchor, false);
            glowVisual.transform.localPosition = Vector3.zero;
            glowVisual.transform.localScale = Vector3.one * 1.6f;
            glowVisual.SetActive(true);
        }

        private void Unsubscribe()
        {
            if (cosmeticSystem != null)
            {
                cosmeticSystem.Equipped -= OnEquippedChanged;
                cosmeticSystem.Unequipped -= OnEquippedChanged;
            }
            if (healthSystem != null)
            {
                healthSystem.HealthChanged -= OnHealthChanged;
                healthSystem.RunDied -= CancelPreview;
            }
            if (evolutionSystem != null)
            {
                evolutionSystem.StageChanged -= OnStageChanged;
            }
            if (rebirthSystem != null)
            {
                rebirthSystem.Completed -= OnRebirthCompleted;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (glowVisual != null) Destroy(glowVisual);
        }
    }
}
