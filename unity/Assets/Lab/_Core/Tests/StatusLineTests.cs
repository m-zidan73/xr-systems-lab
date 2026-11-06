using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class StatusLineTests
    {
        [Test]
        public void Show_KeepsTheLatestMessage()
        {
            var go = new GameObject("Status");
            try
            {
                var status = go.AddComponent<StatusLine>();
                Assert.IsTrue(string.IsNullOrEmpty(status.Message));
                status.Show("First");
                status.Show("Second");
                Assert.AreEqual("Second", status.Message);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
