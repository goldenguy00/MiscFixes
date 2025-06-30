using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MiscFixes.Modules
{
    public class PersistentOverlay : MonoBehaviour
    {
        public Material OverlayMaterial;
        public AssetReferenceT<Material> OverlayMaterialReference;

        private AssetOrDirectReference<Material> _overlayReference;

        private CharacterModel _characterModel;
        private TemporaryOverlayInstance _temporaryOverlay;

        private void Awake()
        {
            _characterModel = GetComponent<CharacterModel>();

            _overlayReference = new AssetOrDirectReference<Material>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = OverlayMaterialReference,
                directRef = OverlayMaterial
            };
        }

        private void OnEnable()
        {
            if (_overlayReference.IsLoaded())
                SetOverlayMaterial(_overlayReference.Result);

            _overlayReference.onValidReferenceDiscovered += OnOverlayMaterialDiscovered;
        }

        private void OnDisable()
        {
            if (_overlayReference is not null)
                _overlayReference.onValidReferenceDiscovered -= OnOverlayMaterialDiscovered;

            SetOverlayMaterial(null);
        }

        private void OnDestroy()
        {
            _overlayReference?.Reset();
            _overlayReference = null;
        }

        public void OnOverlayMaterialDiscovered(Material overlayMaterial)
        {
            SetOverlayMaterial(overlayMaterial);
        }

        public void SetOverlayMaterial(Material overlayMaterial)
        {
            bool enableOverlay = overlayMaterial != null;
            bool hasOverlay = _temporaryOverlay is not null;
            if (enableOverlay == hasOverlay && (_temporaryOverlay is null || _temporaryOverlay.originalMaterial == overlayMaterial))
                return;

            _temporaryOverlay?.CleanupEffect();
            _temporaryOverlay = null;

            if (enableOverlay)
            {
                _temporaryOverlay = TemporaryOverlayManager.AddOverlay(base.gameObject);
                _temporaryOverlay.duration = float.PositiveInfinity;
                _temporaryOverlay.originalMaterial = overlayMaterial;
                _temporaryOverlay.AddToCharacterModel(_characterModel);
            }
        }
    }
}