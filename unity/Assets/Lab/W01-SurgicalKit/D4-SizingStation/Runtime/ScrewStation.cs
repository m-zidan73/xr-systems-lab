using System;
using System.Collections.Generic;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.SizingStation
{
    /// <summary>
    /// The holder a screw lies in while it is sized. When a screw is finished with, the station
    /// starts a new one from the same prefab, so every screw in the scene shares one mesh and one
    /// material. Kept screws go to a rack beside it.
    /// </summary>
    public class ScrewStation : MonoBehaviour
    {
        [SerializeField] ScrewSizer sizer;
        [SerializeField] Screw screwPrefab;
        [Tooltip("The screw in the holder now.")]
        [SerializeField] Screw current;
        [Tooltip("Where the top of the head sits. Its forward is the way the screw points, its up is away from the base.")]
        [SerializeField] Transform cradle;
        [Tooltip("The first rack place. Places run along its right, rows along its forward. A negative spacing runs the other way.")]
        [SerializeField] Transform rack;
        [SerializeField, Min(1)] int rackColumns = 6;
        [SerializeField] float rackColumnMetres = 0.015f;
        [SerializeField] float rackRowMetres = 0.09f;

        readonly List<Screw> _kept = new List<Screw>();
        ScrewSizer _connected;
        int _made = 1;

        /// <summary>Raised when a new screw is put in the holder.</summary>
        public event Action<Screw> Spawned;

        public Screw Current => current;
        public IReadOnlyList<Screw> Kept => _kept;

        void OnEnable() => Connect();
        void OnDisable() => Disconnect();

        /// <summary>Sets the station up in code.</summary>
        public void Load(ScrewSizer withSizer, Screw prefab, Screw inHolder, Transform holder, Transform rackStart)
        {
            Disconnect();
            sizer = withSizer;
            screwPrefab = prefab;
            current = inHolder;
            cradle = holder;
            rack = rackStart;
            Connect();
        }

        void Connect()
        {
            if (_connected != null || sizer == null) return;
            _connected = sizer;
            _connected.Changed += Rest;
            Rest();
        }

        void Disconnect()
        {
            if (_connected != null) _connected.Changed -= Rest;
            _connected = null;
        }

        /// <summary>
        /// Lays the screw in the holder. A wider screw has a wider head, so it is lifted by its head's
        /// radius and always rests on the base, never in it or above it.
        /// </summary>
        public void Rest()
        {
            if (current == null || cradle == null) return;
            current.transform.SetPositionAndRotation(cradle.position + cradle.up * HeadRadius(current), cradle.rotation);
        }

        /// <summary>Keeps the sized screw on the rack and starts a new one in the holder.</summary>
        public void Keep()
        {
            if (current == null) return;
            var kept = current;
            var place = _kept.Count;
            _kept.Add(kept);
            if (rack != null)
            {
                var column = place % rackColumns;
                var row = place / rackColumns;
                var position = rack.position + rack.right * (column * rackColumnMetres) + rack.forward * (row * rackRowMetres);
                kept.transform.SetPositionAndRotation(position + rack.up * HeadRadius(kept), rack.rotation);
            }
            current = null;
            Spawn();
        }

        /// <summary>Puts a new screw in the holder at the sizer's current size.</summary>
        public Screw Spawn()
        {
            if (screwPrefab == null) return null;
            current = Instantiate(screwPrefab, transform);
            _made++;
            current.name = $"Station Screw {_made}";
            if (sizer != null) sizer.Attach(current);
            Rest();
            Spawned?.Invoke(current);
            return current;
        }

        /// <summary>Removes the kept screws and puts the one in the holder back to the start size.</summary>
        public void Restart()
        {
            foreach (var kept in _kept)
            {
                if (kept == null) continue;
                if (Application.isPlaying) Destroy(kept.gameObject);
                else DestroyImmediate(kept.gameObject);
            }
            _kept.Clear();
            if (sizer != null) sizer.ResetAxes();
            Rest();
        }

        static float HeadRadius(Screw screw) =>
            screw.Spec != null ? ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(screw.Spec.diameterMm)) * 0.5f : 0f;
    }
}
