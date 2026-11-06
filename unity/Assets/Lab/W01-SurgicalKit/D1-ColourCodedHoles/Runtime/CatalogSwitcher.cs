using System;
using System.Collections.Generic;
using Lab.Core;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles
{
    /// <summary>
    /// Swaps the whole exercise to another manufacturer's catalog: the hole colours, which sizes
    /// exist, and the screws in the tray. No code changes between manufacturers, only data.
    /// </summary>
    public class CatalogSwitcher : MonoBehaviour
    {
        [SerializeField] ImplantCatalog[] catalogs = new ImplantCatalog[0];
        [SerializeField] Hole[] holes = new Hole[0];
        [Tooltip("One slot per screw in the largest catalog. Slots a smaller catalog doesn't need are hidden.")]
        [SerializeField] Screw[] trayScrews = new Screw[0];
        [SerializeField] float trayLengthMm = 20f;

        int _index = -1;

        public event Action<ImplantCatalog> Switched;

        public IReadOnlyList<ImplantCatalog> Catalogs => catalogs;
        public IReadOnlyList<Hole> Holes => holes;
        public IReadOnlyList<Screw> TrayScrews => trayScrews;
        public ImplantCatalog Current => _index >= 0 && _index < catalogs.Length ? catalogs[_index] : null;

        void Start()
        {
            if (catalogs.Length > 0) Show(0);
        }

        public void Next() => Show(catalogs.Length == 0 ? 0 : (_index + 1) % catalogs.Length);

        public void Show(int index)
        {
            if (catalogs.Length == 0) return;
            _index = Mathf.Clamp(index, 0, catalogs.Length - 1);
            var catalog = catalogs[_index];

            foreach (var hole in holes)
            {
                if (hole == null) continue;
                hole.Vacate();
                hole.SetCatalog(catalog);
            }

            var specs = catalog != null && catalog.screws != null ? catalog.screws : new ScrewSpec[0];
            for (var i = 0; i < trayScrews.Length; i++)
            {
                var screw = trayScrews[i];
                if (screw == null) continue;
                var inUse = i < specs.Length && specs[i] != null;
                if (inUse) screw.Set(specs[i], trayLengthMm);
                var item = screw.GetComponent<Grabbable>();
                if (item != null) item.ReturnHome(true);
                screw.gameObject.SetActive(inUse);
            }

            Switched?.Invoke(catalog);
        }
    }
}
