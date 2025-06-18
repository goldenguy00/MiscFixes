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

        AssetOrDirectReference<Material> _overlayReference;

        CharacterModel _characterModel;
        TemporaryOverlayInstance _temporaryOverlay;

        void Awake()
        {
            _characterModel = GetComponent<CharacterModel>();

            _overlayReference = new AssetOrDirectReference<Material>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = OverlayMaterialReference,
                directRef = OverlayMaterial
            };
        }

        void OnDestroy()
        {
            _overlayReference?.Reset();
            _overlayReference = null;
        }

        void OnEnable()
        {
            if (_overlayReference.IsLoaded())
                setOverlayMaterial(_overlayReference.Result);

            _overlayReference.onValidReferenceDiscovered += onOverlayMaterialDiscovered;
        }

        void OnDisable()
        {
            _overlayReference.onValidReferenceDiscovered -= onOverlayMaterialDiscovered;
            setOverlayMaterial(null);
        }

        void onOverlayMaterialDiscovered(Material overlayMaterial)
        {
            setOverlayMaterial(overlayMaterial);
        }

        void setOverlayMaterial(Material overlayMaterial)
        {
            bool enableOverlay = overlayMaterial;
            bool hasOverlay = _temporaryOverlay != null;
            if (enableOverlay == hasOverlay && (!hasOverlay || _temporaryOverlay.originalMaterial == overlayMaterial))
                return;

            _temporaryOverlay?.CleanupEffect();
            _temporaryOverlay = null;

            if (enableOverlay)
            {
                _temporaryOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                _temporaryOverlay.duration = float.PositiveInfinity;
                _temporaryOverlay.originalMaterial = overlayMaterial;
                _temporaryOverlay.AddToCharacterModel(_characterModel);
            }
        }
    }
}