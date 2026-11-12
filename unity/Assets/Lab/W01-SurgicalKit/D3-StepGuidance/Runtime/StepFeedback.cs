using Lab.Core;
using UnityEngine;

namespace Lab.W01.StepGuidance
{
    /// <summary>
    /// Reports progress on the status line and shows "All steps done" with the mistakes in a banner, so the
    /// end of the procedure can't be missed. Mistakes themselves show in the step panel, beside the step.
    /// </summary>
    public class StepFeedback : MonoBehaviour
    {
        [SerializeField] StepGuide guide;
        [SerializeField] StatusLine status;
        [SerializeField] Banner banner;
        [Tooltip("A line under the summary once every step is done, such as how to start again.")]
        [SerializeField] string afterDone = "";

        StepGuide _connected;
        StatusLine _connectedStatus;
        Banner _connectedBanner;

        void OnEnable() => Connect(guide, status, banner);
        void OnDisable() => Disconnect();

        public void Connect(StepGuide to, StatusLine toStatus, Banner toBanner)
        {
            Disconnect();
            _connected = to;
            _connectedStatus = toStatus;
            _connectedBanner = toBanner;
            if (_connected == null) return;
            _connected.StepChanged += OnStepChanged;
            _connected.Finished += OnFinished;
            _connected.Restarted += OnRestarted;
        }

        public void Disconnect()
        {
            if (_connected != null)
            {
                _connected.StepChanged -= OnStepChanged;
                _connected.Finished -= OnFinished;
                _connected.Restarted -= OnRestarted;
            }
            _connected = null;
        }

        void OnStepChanged(int from, int to)
        {
            if (to < from) Show(StepMessages.Reopened(to, _connected.Current));
            else Show(StepMessages.StepDone(to - 1));
        }

        void OnFinished()
        {
            Show(StepMessages.DoneTitle + ".");
            if (_connectedBanner == null) return;
            var summary = StepMessages.Summary(_connected.Mistakes);
            _connectedBanner.Show(StepMessages.DoneTitle, string.IsNullOrWhiteSpace(afterDone) ? summary : $"{summary}\n{afterDone}");
        }

        void OnRestarted()
        {
            Show(StepMessages.StartedAgain);
            if (_connectedBanner != null) _connectedBanner.Hide();
        }

        void Show(string message)
        {
            if (_connectedStatus != null) _connectedStatus.Show(message);
        }
    }
}
